using System.Security.Cryptography;
using System.Text;

namespace FluentFin.Core;

public static class SecurityExtensions
{
	public static byte[] Protect(this string plainText, byte[] entropy)
	{
		return ProtectedData.Protect(Encoding.UTF8.GetBytes(plainText), entropy, DataProtectionScope.CurrentUser);
	}

	public static string Unprotect(this byte[] protectedData, byte[] entropy)
	{
		return Encoding.UTF8.GetString(ProtectedData.Unprotect(protectedData, entropy, DataProtectionScope.CurrentUser));
	}
}
