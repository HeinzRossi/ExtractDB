using ExtractDB.Core.Naming;

namespace ExtractDB.Core.Tests.Naming;

public sealed class NameConverterTests
{
    private readonly NameConverter converter = new();

    [Theory]
    [InlineData("CLIENTE_ENDERECO", "ClienteEndereco")]
    [InlineData("ID_CLIENTE", "IdCliente")]
    [InlineData("DATA_CADASTRO", "DataCadastro")]
    [InlineData("CPF_CNPJ", "CpfCnpj")]
    [InlineData("URL_IMAGEM", "UrlImagem")]
    [InlineData("XML_NOTA", "XmlNota")]
    [InlineData("UUID_EXTERNO", "UuidExterno")]
    [InlineData("CLIENTES", "Clientes")]
    [InlineData("CLIENTE-ENDERECO", "ClienteEndereco")]
    [InlineData("CLIENTE ENDERECO", "ClienteEndereco")]
    [InlineData("CLIENTE.ENDERECO", "ClienteEndereco")]
    public void ToPascalCase_normalizes_database_names(string databaseName, string expected)
    {
        Assert.Equal(expected, converter.ToPascalCase(databaseName));
    }

    [Theory]
    [InlineData("CLASS", "ClassValue")]
    [InlineData("END", "EndValue")]
    public void ToPascalCase_appends_value_to_reserved_words(string databaseName, string expected)
    {
        Assert.Equal(expected, converter.ToPascalCase(databaseName));
    }

    [Fact]
    public void ToUniquePascalCase_appends_numeric_suffix_for_collisions()
    {
        var names = converter.ToUniquePascalCase(["ID_CLIENTE", "IDCLIENTE", "ID CLIENTE"]);

        Assert.Equal(["IdCliente", "IdCliente2", "IdCliente3"], names);
    }
}
