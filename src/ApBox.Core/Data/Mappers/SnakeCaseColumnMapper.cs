using Dapper;
using System.Globalization;

namespace ApBox.Core.Data.Mappers;

/// <summary>
/// Custom Dapper column mapper that converts snake_case database column names to PascalCase property names
/// </summary>
public class SnakeCaseColumnMapper : SqlMapper.ITypeMap
{
    private readonly SqlMapper.ITypeMap _defaultMapper;
    private readonly Type _type;

    public SnakeCaseColumnMapper(Type type)
    {
        _type = type;
        _defaultMapper = new DefaultTypeMap(type);
    }

    public SqlMapper.IMemberMap? GetConstructorParameter(System.Reflection.ConstructorInfo constructor, string columnName)
    {
        return _defaultMapper.GetConstructorParameter(constructor, ToPascalCase(columnName));
    }

    public SqlMapper.IMemberMap? GetMember(string columnName)
    {
        // First try the original column name
        var member = _defaultMapper.GetMember(columnName);
        if (member != null)
            return member;

        // If not found, try converting snake_case to PascalCase
        var pascalCaseName = ToPascalCase(columnName);
        return _defaultMapper.GetMember(pascalCaseName);
    }

    public System.Reflection.ConstructorInfo? FindConstructor(string[] names, Type[] types)
    {
        var pascalCaseNames = names.Select(ToPascalCase).ToArray();
        return _defaultMapper.FindConstructor(pascalCaseNames, types);
    }

    public System.Reflection.ConstructorInfo? FindExplicitConstructor()
    {
        return _defaultMapper.FindExplicitConstructor();
    }

    /// <summary>
    /// Converts snake_case column names to PascalCase property names
    /// Examples: reader_id -> ReaderId, card_number -> CardNumber, timestamp -> Timestamp
    /// </summary>
    private static string ToPascalCase(string columnName)
    {
        if (string.IsNullOrEmpty(columnName))
            return columnName;

        // Split by underscore and capitalize each part
        var parts = columnName.Split('_', StringSplitOptions.RemoveEmptyEntries);
        var result = string.Join("", parts.Select(part => 
            CultureInfo.InvariantCulture.TextInfo.ToTitleCase(part.ToLowerInvariant())));

        return result;
    }
}

/// <summary>
/// Extension methods to register the snake case mapper with Dapper
/// </summary>
public static class DapperMappingExtensions
{
    /// <summary>
    /// Configure Dapper to use snake_case to PascalCase mapping for the specified types
    /// </summary>
    public static void ConfigureSnakeCaseMapping()
    {
        // Register mapping for our entity types
        SqlMapper.SetTypeMap(typeof(ApBox.Core.Data.Models.CardEventEntity), new SnakeCaseColumnMapper(typeof(ApBox.Core.Data.Models.CardEventEntity)));
        SqlMapper.SetTypeMap(typeof(ApBox.Core.Data.Models.ReaderConfigurationEntity), new SnakeCaseColumnMapper(typeof(ApBox.Core.Data.Models.ReaderConfigurationEntity)));
        SqlMapper.SetTypeMap(typeof(ApBox.Core.Data.Models.PluginConfigurationEntity), new SnakeCaseColumnMapper(typeof(ApBox.Core.Data.Models.PluginConfigurationEntity)));
        SqlMapper.SetTypeMap(typeof(ApBox.Core.Data.Models.FeedbackConfigurationEntity), new SnakeCaseColumnMapper(typeof(ApBox.Core.Data.Models.FeedbackConfigurationEntity)));
    }

    /// <summary>
    /// Configure Dapper mapping for a specific entity type
    /// </summary>
    public static void ConfigureSnakeCaseMapping<T>()
    {
        SqlMapper.SetTypeMap(typeof(T), new SnakeCaseColumnMapper(typeof(T)));
    }
}