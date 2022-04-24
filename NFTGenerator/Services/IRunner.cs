// Copyright Matteo Beltrame

using System.Threading.Tasks;
using System.Threading;

namespace NFTGenerator.Services;

public interface IRunner
{
    Task RunAsync(CancellationToken token);
}