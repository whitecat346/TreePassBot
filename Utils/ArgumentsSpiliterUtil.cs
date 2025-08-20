namespace TreePassBot.Utils;

public class ArgumentsSpiliterUtil
{
    /// <summary>
    /// 将输入的字符串按照空格和换行的方式进行分隔
    /// </summary>
    /// <param name="input">输入的字符串</param>
    /// <returns>分隔结果</returns>
    public IEnumerable<string> SpilitArguments(string input)
    {
        var result = input.Split(['\n', ' '], StringSplitOptions.RemoveEmptyEntries);

        return result;
    }
}