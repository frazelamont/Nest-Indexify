using System.Collections.Generic;

namespace Nest.Indexify.Contributors.Analysis
{
	public abstract class IndexAnalysisContributor<T, TAnalyzer> : ElasticsearchIndexCreationContributor where T : TAnalyzer where TAnalyzer : class
	{
		protected abstract IEnumerable<KeyValuePair<string, T>> Build();

		protected IndexAnalysisContributor(int order = 0) : base(order) { }

		protected virtual bool CanContribute(KeyValuePair<string, T> setting, FluentDictionary<string, T> existing)
		{
			return !existing.ContainsKey(setting.Key);
		}

		public sealed override void ContributeCore(CreateIndexDescriptor descriptor, IElasticClient client)
		{
		    descriptor
		        .Settings(s => s
		            .Analysis(ContributeCore)
		        );
		}

		protected virtual AnalysisDescriptor ContributeCore(AnalysisDescriptor descriptor)
		{
			return Contribute(descriptor, Build());
		}

		protected abstract AnalysisDescriptor Contribute(AnalysisDescriptor descriptor, IEnumerable<KeyValuePair<string, T>> build);
	}
}