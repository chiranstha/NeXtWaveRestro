using System.Collections.Generic;

namespace NextWave.Erp.DataExporting;

public interface IExcelColumnSelectionInput
{
    List<string> SelectedColumns { get; set; }
}

