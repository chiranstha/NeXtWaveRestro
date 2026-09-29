namespace Nextwave.ERP.PrintAgent.Ipp;

public sealed record IppPrintRequest(
    int RequestId,
    string JobName,
    string DocumentFormat,
    byte[] Document);
