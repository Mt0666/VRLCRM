namespace VRLCRM.Models.Settings;

public class CompanyDocumentSettings
{
    public const string SectionName = "CompanyDocument";

    public string CompanyName { get; set; } = "Mete Varol";

    public string LogoRelativePath { get; set; } = "img/mete_varol.png";

    public string Phone { get; set; } = string.Empty;

    public string Website { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string BankName { get; set; } = string.Empty;

    public string BankAccountName { get; set; } = string.Empty;

    public string BankAccountNumber { get; set; } = string.Empty;

    public int PaymentDueDays { get; set; } = 30;
}
