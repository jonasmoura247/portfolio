namespace Bancada.Aplicacao;

/// <summary>
/// Cifra de campo (texto livre do prontuário, motivos). O terceiro argumento amarra o valor ao seu registro:
/// copiar um texto cifrado de uma linha para outra faz a decifragem falhar.
/// </summary>
public interface ICifra
{
    string Cifrar(string texto, string dadosAssociados);

    string Decifrar(string valor, string dadosAssociados);
}
