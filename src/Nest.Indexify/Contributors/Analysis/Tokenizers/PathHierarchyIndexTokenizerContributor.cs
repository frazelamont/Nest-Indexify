using System.Collections.Generic;

namespace Nest.Indexify.Contributors.Analysis.Tokenizers
{
    public class PathHierarchyIndexTokenizerContributor : IndexAnalysisTokenizerContributor
    {
        private readonly string _name;
        private readonly char? _delimiter;
        private readonly bool? _reverse;
        private readonly int? _bufferSize;
        private readonly char? _replacement;
        private readonly int? _skip;

        public PathHierarchyIndexTokenizerContributor(string name = "indexify_path", char? delimiter = null, int? skip = null, char? replacement = null, int? bufferSize = null, bool? reverse = null)
        {
            _name = name;
            _delimiter = delimiter;
            _skip = skip;
            _replacement = replacement;
            _bufferSize = bufferSize;
            _reverse = reverse;
        }

        protected override IEnumerable<KeyValuePair<string, TokenizerBase>> Build()
        {
            yield return new KeyValuePair<string, TokenizerBase>(_name, new PathHierarchyTokenizer()
            {
                Delimiter = _delimiter,
                Reverse = _reverse,
                Skip = _skip,
                Replacement = _replacement,
                BufferSize = _bufferSize
            });
        }
    }
}