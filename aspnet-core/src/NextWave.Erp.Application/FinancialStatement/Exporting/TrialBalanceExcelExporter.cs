using System.Collections.Generic;
using System.Diagnostics;
using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Storage;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NextWave.Erp.ControlPanel.Dtos;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Enums;

namespace NextWave.Erp.FinancialStatement.Exporting;

public class TrialBalanceExcelExporter(ITempFileCacheManager tempFileCacheManager)
    : NpoiExcelExporterBase(tempFileCacheManager), ITrialBalanceExcelExporter
{
    public FileDto ExportToFile(List<FinancialStatementDetail> trialBalance, GetBranchForViewDto branchDto)
    {
        return CreateExcelPackage(
            "TrialBalance.xlsx",
            excelPackage =>
            {
                var sheet = excelPackage.CreateSheet("TrialBalance");

                // prepare some styles
                var headerStyle = excelPackage.CreateCellStyle();
                var headerFont = excelPackage.CreateFont();
                headerFont.IsBold = true;
                headerFont.FontHeightInPoints = 12;
                headerFont.Color = IndexedColors.White.Index;
                headerStyle.SetFont(headerFont);
                headerStyle.Alignment = HorizontalAlignment.Center;
                headerStyle.VerticalAlignment = VerticalAlignment.Center;
                headerStyle.FillForegroundColor = IndexedColors.Teal.Index;
                headerStyle.FillPattern = FillPattern.SolidForeground;

                // Group style - distinct light blue background, bold black text
                var groupStyle = excelPackage.CreateCellStyle();
                var groupFont = excelPackage.CreateFont();
                groupFont.IsBold = true;
                groupFont.Color = IndexedColors.Black.Index;
                groupStyle.SetFont(groupFont);
                groupStyle.FillForegroundColor = IndexedColors.LightCornflowerBlue.Index;
                groupStyle.FillPattern = FillPattern.SolidForeground;
                groupStyle.Alignment = HorizontalAlignment.Left;

                // Subgroup style - distinct light yellow background, normal black text
                var subgroupStyle = excelPackage.CreateCellStyle();
                var subgroupFont = excelPackage.CreateFont();
                subgroupFont.IsBold = false;
                subgroupFont.Color = IndexedColors.Black.Index;
                subgroupStyle.SetFont(subgroupFont);
                subgroupStyle.FillForegroundColor = IndexedColors.LightYellow.Index;
                subgroupStyle.FillPattern = FillPattern.SolidForeground;
                subgroupStyle.Alignment = HorizontalAlignment.Left;

                var normalStyle = excelPackage.CreateCellStyle();
                normalStyle.Alignment = HorizontalAlignment.Left;

                var numberStyle = excelPackage.CreateCellStyle();
                numberStyle.Alignment = HorizontalAlignment.Right;

                int rowIndex = 0;

                // School name (centered, bold, merged)
                var schoolRow = sheet.CreateRow(rowIndex++);
                var schoolCell = schoolRow.CreateCell(0);
                schoolCell.SetCellValue(branchDto?.CompanyName ?? string.Empty);
                var schoolFont = excelPackage.CreateFont();
                schoolFont.IsBold = true;
                schoolFont.FontHeightInPoints = 14;
                var schoolStyle = excelPackage.CreateCellStyle();
                schoolStyle.SetFont(schoolFont);
                schoolStyle.Alignment = HorizontalAlignment.Center;
                schoolStyle.VerticalAlignment = VerticalAlignment.Center;
                schoolCell.CellStyle = schoolStyle;
                sheet.AddMergedRegion(new CellRangeAddress(schoolRow.RowNum, schoolRow.RowNum, 0, 6));

                // Address (centered)
                var addressRow = sheet.CreateRow(rowIndex++);
                var addressCell = addressRow.CreateCell(0);
                addressCell.SetCellValue(branchDto?.Address ?? string.Empty);
                var centerStyle = excelPackage.CreateCellStyle();
                centerStyle.Alignment = HorizontalAlignment.Center;
                centerStyle.VerticalAlignment = VerticalAlignment.Center;
                addressCell.CellStyle = centerStyle;
                sheet.AddMergedRegion(new CellRangeAddress(addressRow.RowNum, addressRow.RowNum, 0, 6));

                // PAN and Contact (centered)
               
               
                // Empty row
                rowIndex++;

                // "Trial Balance" heading (centered, bold)
                var tbRow = sheet.CreateRow(rowIndex++);
                var tbCell = tbRow.CreateCell(0);
                tbCell.SetCellValue("Trial Balance");
                var tbFont = excelPackage.CreateFont();
                tbFont.IsBold = true;
                tbFont.FontHeightInPoints = 12;
                var tbStyle = excelPackage.CreateCellStyle();
                tbStyle.SetFont(tbFont);
                tbStyle.Alignment = HorizontalAlignment.Center;
                tbCell.CellStyle = tbStyle;
                sheet.AddMergedRegion(new CellRangeAddress(tbRow.RowNum, tbRow.RowNum, 0, 6));

                // Empty row before table headers
                rowIndex++;

                // Header grouping row (Opening Balance / Transaction / Closing Balance)
                var headerRow1 = sheet.CreateRow(rowIndex++);
                // ensure the row exists for merged header cells
                for (int c = 0; c <= 6; c++) headerRow1.CreateCell(c);

                // Account Name (no merge)
                var accountNameCell = headerRow1.GetCell(0);
                accountNameCell.SetCellValue("Account Name");
                accountNameCell.CellStyle = headerStyle;

                // Merged groups
                sheet.AddMergedRegion(new CellRangeAddress(headerRow1.RowNum, headerRow1.RowNum, 1, 2));
                var openingCell = headerRow1.GetCell(1);
                openingCell.SetCellValue("Opening Balance");
                openingCell.CellStyle = headerStyle;

                sheet.AddMergedRegion(new CellRangeAddress(headerRow1.RowNum, headerRow1.RowNum, 3, 4));
                var txnCell = headerRow1.GetCell(3);
                txnCell.SetCellValue("Transaction");
                txnCell.CellStyle = headerStyle;

                sheet.AddMergedRegion(new CellRangeAddress(headerRow1.RowNum, headerRow1.RowNum, 5, 6));
                var closingCell = headerRow1.GetCell(5);
                closingCell.SetCellValue("Closing Balance");
                closingCell.CellStyle = headerStyle;

                // Sub header row (Debit / Credit)
                var headerRow2 = sheet.CreateRow(rowIndex++);
                var hName = headerRow2.CreateCell(0);
                hName.SetCellValue("");
                hName.CellStyle = headerStyle;

                var h1 = headerRow2.CreateCell(1);
                h1.SetCellValue("Debit");
                h1.CellStyle = headerStyle;
                var h2 = headerRow2.CreateCell(2);
                h2.SetCellValue("Credit");
                h2.CellStyle = headerStyle;

                var h3 = headerRow2.CreateCell(3);
                h3.SetCellValue("Debit");
                h3.CellStyle = headerStyle;
                var h4 = headerRow2.CreateCell(4);
                h4.SetCellValue("Credit");
                h4.CellStyle = headerStyle;

                var h5 = headerRow2.CreateCell(5);
                h5.SetCellValue("Debit");
                h5.CellStyle = headerStyle;
                var h6 = headerRow2.CreateCell(6);
                h6.SetCellValue("Credit");
                h6.CellStyle = headerStyle;

                // We'll collect row index and indent level for grouping after we write rows.
                var writtenRows = new List<(int RowNum, int Level, TrailBalanceGroupEnum GroupType)>();

                // Data rows
                foreach (var item in trialBalance)
                {
                    var row = sheet.CreateRow(rowIndex++);

                    // compute indent level by counting leading "->" markers (trim leading spaces first)
                    var rawName = item.Name ?? string.Empty;
                    var trimmed = rawName.TrimStart();
                    var level = 0;
                    while (trimmed.StartsWith("->"))
                    {
                        level++;
                        trimmed = trimmed.Substring(2).TrimStart();
                    }

                    // Debug: show account and computed level
                    Debug.WriteLine($"TrialBalance Row {row.RowNum} - RawName='{rawName}', CleanName='{trimmed}', Level={level}, GroupType={item.GroupType}");

                    // create indent style if needed (cache small set if many rows)
                    ICellStyle indentStyle = null;
                    if (level > 0)
                    {
                        indentStyle = excelPackage.CreateCellStyle();
                        indentStyle.Alignment = HorizontalAlignment.Left;
                        // Excel supports indent up to 15; clamp to 15
                        indentStyle.Indention = (short)(level > 15 ? 15 : level);
                    }

                    var cell0 = row.CreateCell(0);
                    cell0.SetCellValue(trimmed);
                    cell0.CellStyle = indentStyle ?? normalStyle;

                    var cell1 = row.CreateCell(1);
                    cell1.SetCellValue((double)item.OpeningDr);

                    var cell2 = row.CreateCell(2);
                    cell2.SetCellValue((double)item.OpeningCr);

                    var cell3 = row.CreateCell(3);
                    cell3.SetCellValue((double)item.Debit);

                    var cell4 = row.CreateCell(4);
                    cell4.SetCellValue((double)item.Credit);

                    var cell5 = row.CreateCell(5);
                    cell5.SetCellValue((double)item.ClosingDr);

                    var cell6 = row.CreateCell(6);
                    cell6.SetCellValue((double)item.ClosingCr);

                    // apply styles based on group type
                    if (level == 0)
                    {
                        // apply group style across the row
                        for (int c = 0; c <= 6; c++)
                        {
                            var cell = row.GetCell(c) ?? row.CreateCell(c);
                            cell.CellStyle = groupStyle;
                        }
                    }
                    else if (level == 2)
                    {
                        // ledger style - indent name already applied, numbers right aligned
                        cell0.CellStyle = indentStyle ?? normalStyle;
                        for (int c = 1; c <= 6; c++)
                        {
                            var cell = row.GetCell(c);
                            cell.CellStyle = numberStyle;
                        }
                    }
                    else
                    {
                        // other types (products, opening stock, totals)
                        for (int c = 0; c <= 6; c++)
                        {
                            var cell = row.GetCell(c) ?? row.CreateCell(c);
                            cell.CellStyle = subgroupStyle;
                        }
                    }

                    // save row number and computed level for grouping
                    writtenRows.Add((row.RowNum, level, item.GroupType));
                }

                // After writing all rows: create outline groups using computed levels.
                // For each parent row i, group subsequent rows with strictly greater level.
                for (int i = 0; i < writtenRows.Count; i++)
                {
                    var parent = writtenRows[i];
                    var parentLevel = parent.Level;
                    var startIndex = i + 1;
                    if (startIndex >= writtenRows.Count) continue;

                    // find end index where level becomes <= parentLevel
                    var j = startIndex;
                    while (j < writtenRows.Count && writtenRows[j].Level > parentLevel) j++;

                    if (j - 1 >= startIndex)
                    {
                        var startRowNum = writtenRows[startIndex].RowNum;
                        var endRowNum = writtenRows[j - 1].RowNum;

                        // create Excel grouping for that range
                        sheet.GroupRow(startRowNum, endRowNum);

                        // collapse the group by default (best-effort; API/behaviour may vary)
                        try
                        {
                            sheet.SetRowGroupCollapsed(startRowNum, true);
                        }
                        catch
                        {
                            // ignore if API differs for workbook type
                        }

                        Debug.WriteLine($"Grouped rows {startRowNum}..{endRowNum} under parent row {parent.RowNum} (parentLevel={parentLevel})");

                        // skip processed child rows
                        i = j - 1;
                    }
                }

                // Autosize columns
                for (var i = 0; i <= 6; i++)
                    sheet.AutoSizeColumn(i);

                // Ensure opening balance columns are wide enough
                var openingColWidth = 18 * 256;
                sheet.SetColumnWidth(1, openingColWidth);
                sheet.SetColumnWidth(2, openingColWidth);
            });
    }
}