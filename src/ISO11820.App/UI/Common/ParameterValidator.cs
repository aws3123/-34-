namespace ISO11820.App.UI.Common;

/// <summary>
/// 仿真参数验证器，确保输入值在合理范围内
/// </summary>
public static class ParameterValidator
{
    public static (bool IsValid, string[] Errors) Validate(
        double heatingRate,
        double targetTemp,
        double stableThreshold,
        double fluctuation)
    {
        var errors = new List<string>();

        if (heatingRate <= 0 || heatingRate > 200)
        {
            errors.Add("升温速率必须大于 0 且不超过 200 °C/s");
        }

        if (targetTemp <= 0 || targetTemp > 1200)
        {
            errors.Add("目标温度必须大于 0 且不超过 1200 °C");
        }

        if (stableThreshold <= 0 || stableThreshold > 50)
        {
            errors.Add("稳定阈值必须大于 0 且不超过 50 °C");
        }

        if (fluctuation <= 0 || fluctuation > 10)
        {
            errors.Add("温度波动范围必须大于 0 且不超过 10 °C");
        }

        return (errors.Count == 0, errors.ToArray());
    }
}
