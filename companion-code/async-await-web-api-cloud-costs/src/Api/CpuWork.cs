using System.Security.Cryptography;
using System.Text;

namespace Api;

/// <summary>
/// Fixed, deterministic CPU-bound work with no I/O, used by /cpu-bound as the control
/// endpoint where async/await is not expected to help. Iteration count was calibrated
/// on the benchmark host to take roughly 50ms of CPU time per call; see README.
/// </summary>
public static class CpuWork
{
    public const int Iterations = 120_000;

    public static int HashLoop()
    {
        var data = Encoding.UTF8.GetBytes("async-await-web-api-cloud-costs-benchmark");
        var hash = data;
        for (var i = 0; i < Iterations; i++)
        {
            hash = SHA256.HashData(hash);
        }

        return hash.Length;
    }
}
