#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Builder;

namespace DbQueue.Rest
{
    public sealed class EndpointConventionBuilder : IEndpointConventionBuilder
    {
        internal EndpointConventionBuilder(List<IEndpointConventionBuilder> endpointConventionBuilders)
        {
            _endpointConventionBuilders = endpointConventionBuilders;
        }

        readonly List<IEndpointConventionBuilder> _endpointConventionBuilders;

        public void Add(Action<EndpointBuilder> convention)
        {
            _endpointConventionBuilders.ForEach(x => x.Add(convention));
        }
    }
}
#endif