using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExtractDB.Generated.Models;

[Table("sysdiagrams", Schema = "dbo")]
public sealed class Sysdiagrams
{
    [Column("name")]
    [Required]
    public object Name { get; set; } = new();

    [Column("principal_id")]
    public int PrincipalId { get; set; }

    [Column("diagram_id")]
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int DiagramId { get; set; }

    [Column("version")]
    public int? Version { get; set; }

    [Column("definition")]
    public byte[]? Definition { get; set; }

}
