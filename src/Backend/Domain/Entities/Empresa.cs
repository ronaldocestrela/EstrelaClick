using System.Net.Mail;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Domain.Entities;

public sealed class Empresa
{
    private Empresa(
        Guid id,
        string razaoSocial,
        string email,
        string telefone,
        string? identityUserId,
        DateTime dataCadastroUtc)
    {
        Id = id;
        RazaoSocial = razaoSocial;
        Email = email;
        Telefone = telefone;
        IdentityUserId = identityUserId;
        StatusAtivo = true;
        DataCadastroUtc = dataCadastroUtc;
    }

    public Guid Id { get; private set; }

    public string RazaoSocial { get; private set; }

    public string Email { get; private set; }

    public string Telefone { get; private set; }

    public bool StatusAtivo { get; private set; }

    public DateTime DataCadastroUtc { get; private set; }

    public string? IdentityUserId { get; private set; }

    public static Result<Empresa> Criar(string razaoSocial, string email, string telefone, string? identityUserId = null)
    {
        if (string.IsNullOrWhiteSpace(razaoSocial))
        {
            return Result<Empresa>.Failure(new Error("empresa.razaoSocial.invalida", "Razão social é obrigatória."));
        }

        if (string.IsNullOrWhiteSpace(email) || !EmailEhValido(email))
        {
            return Result<Empresa>.Failure(new Error("empresa.email.invalido", "Email informado é inválido."));
        }

        if (string.IsNullOrWhiteSpace(telefone))
        {
            return Result<Empresa>.Failure(new Error("empresa.telefone.invalido", "Telefone é obrigatório."));
        }

        var empresa = new Empresa(
            id: Guid.NewGuid(),
            razaoSocial: razaoSocial.Trim(),
            email: email.Trim(),
            telefone: telefone.Trim(),
            identityUserId: string.IsNullOrWhiteSpace(identityUserId) ? null : identityUserId.Trim(),
            dataCadastroUtc: DateTime.UtcNow);

        return Result<Empresa>.Success(empresa);
    }

    private static bool EmailEhValido(string email)
    {
        return MailAddress.TryCreate(email, out _);
    }
}
