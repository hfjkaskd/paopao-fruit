using System;
using System.IO;
using System.Linq;
using Obfuz.Utils;
using Obfuz.EncryptionVM;
public static class VerifyObfuz {
    public static int Main(string[] args) {
        string project=args[0];
        byte[] expected=KeyGenerator.GenerateKey("com.webpack.picturemerge",VirtualMachine.SecretKeyLength);
        bool stat=expected.SequenceEqual(File.ReadAllBytes(Path.Combine(project,"Assets/Resources/Obfuz/defaultStaticSecretKey.bytes")));
        bool dyn=expected.SequenceEqual(File.ReadAllBytes(Path.Combine(project,"Assets/Resources/Obfuz/defaultDynamicSecretKey.bytes")));
        bool vm=new VirtualMachineCodeGenerator("picturemerge",256).ValidateMatch(Path.Combine(project,"Assets/Obfuz/GeneratedEncryptionVirtualMachine.cs"));
        Console.WriteLine("{\"static_key_matches\":"+stat.ToString().ToLowerInvariant()+",\"dynamic_key_matches\":"+dyn.ToString().ToLowerInvariant()+",\"vm_matches\":"+vm.ToString().ToLowerInvariant()+",\"key_bytes\":"+expected.Length+"}");
        return stat && dyn && vm ? 0:1;
    }
}
