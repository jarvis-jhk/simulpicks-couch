// Checks that a mod built against the reference stubs in refs/ only uses game API that exists.
//
// The stubs are hand-written, so a wrong return type or a field declared as a property would
// compile fine and then throw MissingMethodException in the game. This reads the built mod's
// metadata and checks every type and member it references in a stubbed assembly:
//
//   refcheck <mod.dll> --witness <dll>... [--attested <file>] [--real <Managed dir>]
//   refcheck --dump <dll>      every stubbed-assembly reference a witness makes, to find proof
//
// Witnesses are assemblies compiled against the REAL game: MMHOOK.Rounds (a hook delegate for
// every Assembly-CSharp method, with its exact signature) and mods such as UnboundLib, whose own
// member references are proof that a member exists. Anything no witness covers must be listed in
// the attested file with a reason, and is reported as unproven. With --real the game's own
// assemblies are searched instead, which is the authoritative check.
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

static class P
{
    static readonly HashSet<string> Stubbed = new() {
        "Assembly-CSharp", "Photon3Unity3D", "PhotonRealtime", "PhotonUnityNetworking",
        "Unity.TextMeshPro", "UnityEngine.UI", "InControl" };

    static int Main(string[] args)
    {
        string mod = null, attestedFile = null, real = null;
        var witnesses = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--witness") { while (i + 1 < args.Length && !args[i + 1].StartsWith("--")) witnesses.Add(args[++i]); }
            else if (args[i] == "--attested") attestedFile = args[++i];
            else if (args[i] == "--real") real = args[++i];
            else if (args[i] == "--dump") { foreach (var r in Refs(args[++i]).Distinct()) Console.WriteLine(r.Key); return 0; }
            else mod = args[i];
        }
        if (mod == null) { Console.Error.WriteLine("usage: refcheck <mod.dll> --witness <dll>... [--attested <file>] [--real <dir>]"); return 2; }

        var used = Refs(mod).Where(r => Stubbed.Contains(r.Asm)).Distinct().OrderBy(r => r.Key).ToList();

        if (real != null) return CheckReal(used, real);

        var known = new HashSet<string>();
        foreach (var w in witnesses)
        {
            foreach (var r in Refs(w)) known.Add(r.Key);
            foreach (var k in Hooks(w)) known.Add(k);
        }

        var attested = new Dictionary<string, string>();
        if (attestedFile != null)
            foreach (var line in File.ReadAllLines(attestedFile))
            {
                var t = line.Trim();
                if (t.Length == 0 || t.StartsWith("#")) continue;
                var cut = t.IndexOf("  #", StringComparison.Ordinal);
                attested[(cut < 0 ? t : t[..cut]).Trim()] = cut < 0 ? "" : t[(cut + 3)..].Trim();
            }

        int bad = 0, vouched = 0;
        foreach (var r in used)
        {
            if (known.Contains(r.Key)) continue;
            if (attested.ContainsKey(r.Key)) { vouched++; continue; }
            Console.WriteLine("UNPROVEN " + r.Key);
            bad++;
        }
        foreach (var k in attested.Keys.Where(k => known.Contains(k)))
            Console.WriteLine("note: now witnessed, can be dropped from the attested list: " + k);

        Console.WriteLine($"{used.Count} stubbed references, {used.Count - bad - vouched} witnessed, {vouched} attested by hand, {bad} unproven.");
        return bad == 0 ? 0 : 1;
    }

    record Ref(string Asm, string Key);

    /// <summary>Type and member references, keyed "asm | Type" or "asm | Type | member sig".</summary>
    static IEnumerable<Ref> Refs(string path)
    {
        using var fs = File.OpenRead(path);
        using var pe = new PEReader(fs);
        var md = pe.GetMetadataReader();
        var prov = new Prov(md);
        var list = new List<Ref>();

        foreach (var th in md.TypeReferences)
        {
            var asm = prov.AsmOf(th);
            if (asm != null) list.Add(new Ref(asm, asm + " | " + prov.Name(th)));
        }
        foreach (var h in md.MemberReferences)
        {
            var mr = md.GetMemberReference(h);
            if (mr.Parent.Kind != HandleKind.TypeReference) continue;   // members of generic instantiations are BCL here
            var parent = (TypeReferenceHandle)mr.Parent;
            var asm = prov.AsmOf(parent);
            if (asm == null) continue;
            var n = md.GetString(mr.Name);
            string s;
            if (mr.GetKind() == MemberReferenceKind.Field) s = "field " + mr.DecodeFieldSignature(prov, null) + " " + n;
            else
            {
                var sig = mr.DecodeMethodSignature(prov, null);
                s = (sig.Header.IsInstance ? "" : "static ") + sig.ReturnType + " " + n + "(" + string.Join(", ", sig.ParameterTypes) + ")";
            }
            list.Add(new Ref(asm, asm + " | " + prov.Name(parent) + " | " + s));
        }
        return list;
    }

    /// <summary>MMHOOK's orig_ delegates, turned back into the member references they prove.</summary>
    static IEnumerable<string> Hooks(string path)
    {
        using var fs = File.OpenRead(path);
        using var pe = new PEReader(fs);
        var md = pe.GetMetadataReader();
        var prov = new Prov(md);
        var hookedAsm = md.AssemblyReferences.Select(a => md.GetString(md.GetAssemblyReference(a).Name)).FirstOrDefault(n => n == "Assembly-CSharp");
        if (hookedAsm == null) yield break;

        foreach (var th in md.TypeDefinitions)
        {
            var td = md.GetTypeDefinition(th);
            var name = md.GetString(td.Name);
            if (!name.StartsWith("orig_")) continue;

            // On.<Namespace>.<Type> is the hooked type; nesting is On.Outer.Inner.
            var parts = new List<string>();
            for (var cur = td.GetDeclaringType(); !cur.IsNil; cur = md.GetTypeDefinition(cur).GetDeclaringType())
            {
                var o = md.GetTypeDefinition(cur);
                var ns = md.GetString(o.Namespace);
                parts.Insert(0, md.GetString(o.Name));
                if (ns != "") { parts.Insert(0, ns); break; }
            }
            if (parts.Count == 0 || parts[0] != "On" && !parts[0].StartsWith("On.")) continue;
            var full = string.Join(".", parts);
            full = full.StartsWith("On.") ? full[3..] : full;

            var inv = td.GetMethods().Select(md.GetMethodDefinition).FirstOrDefault(m => md.GetString(m.Name) == "Invoke");
            var pnames = inv.GetParameters().Select(md.GetParameter).Where(p => p.SequenceNumber > 0).OrderBy(p => p.SequenceNumber).Select(p => md.GetString(p.Name)).ToList();
            var sig = inv.DecodeSignature(prov, null);
            var ps = sig.ParameterTypes.ToList();

            bool instance = pnames.Count > 0 && pnames[0] == "self";
            if (instance) ps.RemoveAt(0);

            // Overloads are disambiguated as Name_Type_Type; the real name is the shortest prefix
            // the signature does not contradict, so emit every candidate and let the lookup decide.
            var mname = name[5..];
            if (mname == "ctor") mname = ".ctor";
            var candidates = new List<string> { mname };
            for (int i = mname.IndexOf('_', 1); i > 0; i = mname.IndexOf('_', i + 1)) candidates.Add(mname[..i]);

            // A nested type's hook owner is written Outer.Inner; metadata writes Outer/Inner.
            foreach (var owner in OwnerSpellings(full))
                foreach (var c in candidates)
                    yield return hookedAsm + " | " + owner + " | " + (instance ? "" : "static ") + sig.ReturnType + " " + c + "(" + string.Join(", ", ps) + ")";
        }
    }

    static IEnumerable<string> OwnerSpellings(string dotted)
    {
        yield return dotted;
        var bits = dotted.Split('.');
        for (int i = 1; i < bits.Length; i++)
            yield return string.Join(".", bits[..i]) + "/" + string.Join("/", bits[i..]);
    }

    /// <summary>Authoritative mode: resolve every reference against the game's own assemblies.</summary>
    static int CheckReal(List<Ref> used, string dir)
    {
        var defs = new HashSet<string>();
        foreach (var asm in Stubbed)
        {
            var path = Path.Combine(dir, asm + ".dll");
            if (!File.Exists(path)) { Console.WriteLine("missing " + path); continue; }
            using var fs = File.OpenRead(path);
            using var pe = new PEReader(fs);
            var md = pe.GetMetadataReader();
            var prov = new Prov(md);
            foreach (var th in md.TypeDefinitions)
            {
                var td = md.GetTypeDefinition(th);
                var tname = prov.DefName(th);
                defs.Add(asm + " | " + tname);
                foreach (var fh in td.GetFields())
                {
                    var f = md.GetFieldDefinition(fh);
                    defs.Add(asm + " | " + tname + " | field " + f.DecodeSignature(prov, null) + " " + md.GetString(f.Name));
                }
                foreach (var mh in td.GetMethods())
                {
                    var m = md.GetMethodDefinition(mh);
                    var sig = m.DecodeSignature(prov, null);
                    defs.Add(asm + " | " + tname + " | " + (sig.Header.IsInstance ? "" : "static ") + sig.ReturnType + " " + md.GetString(m.Name) + "(" + string.Join(", ", sig.ParameterTypes) + ")");
                }
            }
        }
        // Inherited members: a reference may name a derived type. Rare in practice, so report
        // those for a human rather than modelling base-type walks.
        int bad = 0;
        foreach (var r in used)
            if (!defs.Contains(r.Key)) { Console.WriteLine("NOT FOUND " + r.Key); bad++; }
        Console.WriteLine($"{used.Count} stubbed references checked against the game, {bad} not found.");
        return bad == 0 ? 0 : 1;
    }
}

class Prov : ISignatureTypeProvider<string, object>
{
    readonly MetadataReader md;
    public Prov(MetadataReader m) { md = m; }

    public string AsmOf(TypeReferenceHandle h)
    {
        EntityHandle rs = md.GetTypeReference(h).ResolutionScope;
        while (rs.Kind == HandleKind.TypeReference) rs = md.GetTypeReference((TypeReferenceHandle)rs).ResolutionScope;
        return rs.Kind == HandleKind.AssemblyReference ? md.GetString(md.GetAssemblyReference((AssemblyReferenceHandle)rs).Name) : null;
    }

    public string Name(TypeReferenceHandle h)
    {
        var tr = md.GetTypeReference(h);
        var ns = md.GetString(tr.Namespace);
        var n = md.GetString(tr.Name);
        if (tr.ResolutionScope.Kind == HandleKind.TypeReference) return Name((TypeReferenceHandle)tr.ResolutionScope) + "/" + n;
        return (ns == "" ? "" : ns + ".") + n;
    }

    public string DefName(TypeDefinitionHandle h)
    {
        var t = md.GetTypeDefinition(h);
        var outer = t.GetDeclaringType();
        if (!outer.IsNil) return DefName(outer) + "/" + md.GetString(t.Name);
        var ns = md.GetString(t.Namespace);
        return (ns == "" ? "" : ns + ".") + md.GetString(t.Name);
    }

    public string GetArrayType(string e, ArrayShape s) => e + "[" + new string(',', s.Rank - 1) + "]";
    public string GetByReferenceType(string e) => "ref " + e;
    public string GetFunctionPointerType(MethodSignature<string> s) => "fnptr";
    public string GetGenericInstantiation(string g, ImmutableArray<string> a) => g + "<" + string.Join(",", a) + ">";
    public string GetGenericMethodParameter(object c, int i) => "!!" + i;
    public string GetGenericTypeParameter(object c, int i) => "!" + i;
    public string GetModifiedType(string m, string u, bool r) => u;
    public string GetPinnedType(string e) => e;
    public string GetPointerType(string e) => e + "*";
    public string GetPrimitiveType(PrimitiveTypeCode t) => t.ToString().ToLowerInvariant();
    public string GetSZArrayType(string e) => e + "[]";
    public string GetTypeFromDefinition(MetadataReader r, TypeDefinitionHandle h, byte k) => new Prov(r).DefName(h);
    public string GetTypeFromReference(MetadataReader r, TypeReferenceHandle h, byte k) => new Prov(r).Name(h);
    public string GetTypeFromSpecification(MetadataReader r, object c, TypeSpecificationHandle h, byte k) => r.GetTypeSpecification(h).DecodeSignature(this, c);
}
