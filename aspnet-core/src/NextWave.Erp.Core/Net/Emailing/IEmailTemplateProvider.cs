namespace NextWave.Erp.Net.Emailing;

public interface IEmailTemplateProvider
{
    string GetDefaultTemplate(int? tenantId);
}

