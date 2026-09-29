using ExtractDB.Generated.Metadata;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExtractDB.Generated.Models;

[Table("SincronizacaoLote", Schema = "dbo")]
public sealed class Sincronizacaolote
{
    [Column("Id")]
    [Key]
    public Guid Id { get; set; }

    [Column("EmpresaCodigo")]
    [Required]
    [MaxLength(50)]
    public string Empresacodigo { get; set; } = string.Empty;

    [Column("Sistema")]
    [Required]
    [MaxLength(150)]
    public string Sistema { get; set; } = string.Empty;

    [Column("DataInicio")]
    public DateTime Datainicio { get; set; }

    [Column("DataFim")]
    public DateTime? Datafim { get; set; }

    [Column("Status")]
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    [Column("TotalRegistros")]
    [DatabaseDefault("((0))")]
    public int Totalregistros { get; set; }

    [Column("TotalEnviados")]
    [DatabaseDefault("((0))")]
    public int Totalenviados { get; set; }

    [Column("TotalErros")]
    [DatabaseDefault("((0))")]
    public int Totalerros { get; set; }

    [Column("MensagemErro")]
    public string? Mensagemerro { get; set; }

    [Column("CriadoEm")]
    [DatabaseDefault("(getdate())")]
    public DateTime Criadoem { get; set; }

}
