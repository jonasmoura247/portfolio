using System;
using System.Security.Cryptography;
using System.Text;
using Bancada.Aplicacao;

namespace Bancada.Infraestrutura;

/// <summary>
/// AES-256-GCM da própria plataforma. Formato guardado: base64(versao(1) | nonce(12) | tag(16) | cifrado).
/// A chave é recebida por referência e zerada ao descartar (uma chave por sessão, só em memória).
/// </summary>
public sealed class CifraAesGcm : ICifra, IDisposable
{
    public const int TamanhoDaChave = 32;
    private const byte Versao = 1;
    private const int TamanhoNonce = 12;
    private const int TamanhoTag = 16;

    private readonly byte[] _chave;
    private bool _descartada;

    public CifraAesGcm(byte[] chave)
    {
        ArgumentNullException.ThrowIfNull(chave);
        if (chave.Length != TamanhoDaChave)
        {
            throw new ArgumentException($"A chave deve ter {TamanhoDaChave} bytes.", nameof(chave));
        }

        _chave = chave;
    }

    public static byte[] NovaChave() => RandomNumberGenerator.GetBytes(TamanhoDaChave);

    public string Cifrar(string texto, string dadosAssociados)
    {
        ArgumentNullException.ThrowIfNull(texto);
        ArgumentNullException.ThrowIfNull(dadosAssociados);
        ObjectDisposedException.ThrowIf(_descartada, this);

        var claro = Encoding.UTF8.GetBytes(texto);
        var saida = new byte[1 + TamanhoNonce + TamanhoTag + claro.Length];
        saida[0] = Versao;
        var nonce = saida.AsSpan(1, TamanhoNonce);
        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_chave, TamanhoTag);
        aes.Encrypt(nonce, claro, saida.AsSpan(1 + TamanhoNonce + TamanhoTag), saida.AsSpan(1 + TamanhoNonce, TamanhoTag), Encoding.UTF8.GetBytes(dadosAssociados));
        return Convert.ToBase64String(saida);
    }

    public string Decifrar(string valor, string dadosAssociados)
    {
        ArgumentNullException.ThrowIfNull(valor);
        ArgumentNullException.ThrowIfNull(dadosAssociados);
        ObjectDisposedException.ThrowIf(_descartada, this);

        var bytes = Convert.FromBase64String(valor);
        if (bytes.Length < 1 + TamanhoNonce + TamanhoTag || bytes[0] != Versao)
        {
            throw new FormatException("Valor cifrado em formato desconhecido.");
        }

        var nonce = bytes.AsSpan(1, TamanhoNonce);
        var tag = bytes.AsSpan(1 + TamanhoNonce, TamanhoTag);
        var cifrado = bytes.AsSpan(1 + TamanhoNonce + TamanhoTag);
        var claro = new byte[cifrado.Length];

        using var aes = new AesGcm(_chave, TamanhoTag);
        aes.Decrypt(nonce, cifrado, tag, claro, Encoding.UTF8.GetBytes(dadosAssociados));
        return Encoding.UTF8.GetString(claro);
    }

    public void Dispose()
    {
        if (_descartada)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(_chave);
        _descartada = true;
    }
}
