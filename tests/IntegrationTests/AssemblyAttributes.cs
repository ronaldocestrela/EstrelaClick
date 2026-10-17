using Xunit;

// Desabilita paralelismo nos testes de integração para evitar concorrência
// com contêineres Docker (MsSqlContainer) e estado estático do Serilog (Log.Logger).
[assembly: CollectionBehavior(DisableTestParallelization = true)]
