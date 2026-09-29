using ExtractDB.Generated.Metadata;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExtractDB.Generated.Models;

[Table("TelaAcessoEventoCentral", Schema = "dbo")]
public sealed class Telaacessoeventocentral
{
    [Column("Id")]
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Column("IdLocal")]
    public long Idlocal { get; set; }

    [Column("IdTela")]
    public long? Idtela { get; set; }

    [Column("EmpresaCodigo")]
    [Required]
    [MaxLength(50)]
    public string Empresacodigo { get; set; } = string.Empty;

    [Column("Sistema")]
    [Required]
    [MaxLength(150)]
    public string Sistema { get; set; } = string.Empty;

    [Column("Modulo")]
    [MaxLength(150)]
    public string? Modulo { get; set; }

    [Column("NomeTela")]
    [Required]
    [MaxLength(150)]
    public string Nometela { get; set; } = string.Empty;

    [Column("TituloTela")]
    [MaxLength(250)]
    public string? Titulotela { get; set; }

    [Column("Usuario")]
    [MaxLength(150)]
    public string? Usuario { get; set; }

    [Column("Evento")]
    [Required]
    [MaxLength(50)]
    public string Evento { get; set; } = string.Empty;

    [Column("DataHora")]
    public DateTime Datahora { get; set; }

    [Column("TempoPermanenciaSegundos")]
    public int? Tempopermanenciasegundos { get; set; }

    [Column("SessaoId")]
    public Guid? Sessaoid { get; set; }

    [Column("Maquina")]
    [MaxLength(150)]
    public string? Maquina { get; set; }

    [Column("VersaoSistema")]
    [MaxLength(50)]
    public string? Versaosistema { get; set; }

    [Column("LoteSincronizacao")]
    public Guid Lotesincronizacao { get; set; }

    [Column("DataRecebimento")]
    [DatabaseDefault("(getdate())")]
    public DateTime Datarecebimento { get; set; }

    [Column("Uf")]
    [MaxLength(2)]
    public string? Uf { get; set; }

}
