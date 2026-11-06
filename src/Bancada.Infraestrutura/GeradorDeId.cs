using System;
using System.Security.Cryptography;
using Bancada.Aplicacao;

namespace Bancada.Infraestrutura;

/// <summary>Id de sessão: 256 bits aleatórios em base64url (43 caracteres), imprevisível e seguro para cookie.</summary>
public sealed class GeradorDeId : IGeradorDeId
{
    public const int Bytes = 32;

    public string Novo() => Base64Url(RandomNumberGenerator.GetBytes(Bytes));

    public static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
