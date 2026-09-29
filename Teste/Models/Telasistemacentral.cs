using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExtractDB.Generated.Models;

[Table("TelaSistemaCentral", Schema = "dbo")]
public sealed class Telasistemacentral
{
    [Column("IdTelaOrigem")]
    public int? Idtelaorigem { get; set; }

    [Column("NomeTela")]
    [MaxLength(50)]
    public string? Nometela { get; set; }

    [Column("Sistema")]
    [MaxLength(50)]
    public string? Sistema { get; set; }

    [Column("Ativa")]
    public int? Ativa { get; set; }

    [Column("Modulo")]
    [MaxLength(50)]
    public string? Modulo { get; set; }

    [Column("TituloTela")]
    [MaxLength(50)]
    public string? Titulotela { get; set; }

}
