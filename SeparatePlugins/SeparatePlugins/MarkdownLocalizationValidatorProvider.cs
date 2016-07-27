namespace Microsoft.DocAsCode.Sample.Plugins
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Web;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.MarkdownLite;

    [Export(ContractName, typeof(IMarkdownTokenValidatorProvider))]
    public class MarkdownLocalizationValidatorProvider : IMarkdownTokenValidatorProvider
    {
        public const string ContractName = "sample_plugins";
        public readonly char[] SpecialCharacters = { '#', '$', '^', '&', '%', '+' };
        public const string SpecialCharacterErrorMessageTemplate = "Sample plugins warning: Special Character found here:'{0}'. Please review and block the text if it should not be localized.";
        public const string PathErrorMessageTemplate = "Sample plugins warning: Seems like a path here:'{0}'. Please review and block the text if it should not be localized.";

        // unix path regex: / following with any character excluding /, space, \n and , 
        private readonly Regex unixPathRegex = new Regex(@"\/([^\/\s\,\n]+(\/)?)+", RegexOptions.Compiled);

        // web path regex: from https://msdn.microsoft.com/en-us/library/ff650303.aspx#paght000001_commonregularexpressions
        private readonly Regex webPathRegex = new Regex(@"((ht|f)tp(s?)\:\/\/[0-9a-zA-Z]|www\.)([-\.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&amp;%\$#_]*)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public ImmutableArray<IMarkdownTokenValidator> GetValidators()
        {
            return ImmutableArray.Create(SpecialCharacterValidator(), PathValidator());
        }

        private IMarkdownTokenValidator SpecialCharacterValidator()
        {
            return MarkdownTokenValidatorFactory.FromLambda<MarkdownTextToken>(
                    token =>
                    {
                        string text = HttpUtility.HtmlDecode((token as MarkdownTextToken).Content);
                        if (text.IndexOfAny(SpecialCharacters) >= 0)
                        {
                            Logger.LogWarning(
                                string.Format(SpecialCharacterErrorMessageTemplate, token.SourceInfo.Markdown),
                                file: token.SourceInfo.File,
                                line: token.SourceInfo.LineNumber.ToString());
                        }
                    });
        }

        private IMarkdownTokenValidator PathValidator()
        {
            return MarkdownTokenValidatorFactory.FromLambda<IMarkdownToken>(
                    token =>
                    {
                        if (token is MarkdownTextToken)
                        {
                            string text = HttpUtility.HtmlDecode((token as MarkdownTextToken).Content);

                            // Check for Web path
                            if (webPathRegex.Match(text).Success)
                            {
                                Logger.LogWarning(
                                    string.Format(PathErrorMessageTemplate, webPathRegex.Match(text).Value),
                                    file: token.SourceInfo.File,
                                    line: token.SourceInfo.LineNumber.ToString());
                            }
                        }
                    });
        }
    }
}
