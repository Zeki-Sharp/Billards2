public class SkillVariant
{
    public SkillConfig BaseConfig { get; }
    public string Tag { get; }
    public string VariantId { get; }

    public SkillVariant(SkillConfig baseConfig, string tag)
    {
        BaseConfig = baseConfig;
        Tag = tag ?? string.Empty;
        VariantId = baseConfig != null ? $"{baseConfig.skillName}@{Tag}" : string.Empty;
    }
}
