using Abp.AspNetZeroCore.Net;
using Abp.Collections.Extensions;
using Abp.Dependency;
using Abp.Extensions;
using NextWave.Erp.Dto;
using NextWave.Erp.Purchase.Dtos;
using NextWave.Erp.Storage;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace NextWave.Erp.DataExporting.Excel.NPOI
{

    public abstract class NpoiExcelExporterBase(ITempFileCacheManager tempFileCacheManager)
        : ERPServiceBase, ITransientDependency
    {
        private readonly Dictionary<string, ICellStyle> _dateCellStyles = new();
        private readonly Dictionary<string, IDataFormat> _dateDateDataFormats = new();
        private IWorkbook _workbook;

        private ICellStyle GetDateCellStyle(ICell cell, string dateFormat)
        {
            if (_workbook != cell.Sheet.Workbook)
            {
                _dateCellStyles.Clear();
                _dateDateDataFormats.Clear();
                _workbook = cell.Sheet.Workbook;
            }

            if (_dateCellStyles.ContainsKey(dateFormat)) return _dateCellStyles.GetValueOrDefault(dateFormat);

            var cellStyle = cell.Sheet.Workbook.CreateCellStyle();
            _dateCellStyles.Add(dateFormat, cellStyle);
            return cellStyle;
        }

        private IDataFormat GetDateDataFormat(ICell cell, string dateFormat)
        {
            if (_workbook != cell.Sheet.Workbook)
            {
                _dateDateDataFormats.Clear();
                _workbook = cell.Sheet.Workbook;
            }

            if (_dateDateDataFormats.ContainsKey(dateFormat)) return _dateDateDataFormats.GetValueOrDefault(dateFormat);

            var dataFormat = cell.Sheet.Workbook.CreateDataFormat();
            _dateDateDataFormats.Add(dateFormat, dataFormat);
            return dataFormat;
        }

        protected FileDto CreateExcelPackage(string fileName, Action<XSSFWorkbook> creator)
        {
            var file = new FileDto(fileName, MimeTypeNames.ApplicationVndOpenxmlformatsOfficedocumentSpreadsheetmlSheet);
            var workbook = new XSSFWorkbook();

            creator(workbook);

            Save(workbook, file);

            return file;
        }

        protected void AddHeader(ISheet sheet, params string[] headerTexts)
        {
            if (headerTexts.IsNullOrEmpty()) return;

            sheet.CreateRow(0);

            for (var i = 0; i < headerTexts.Length; i++) AddHeader(sheet, i, headerTexts[i]);
        }

        protected void AddData(ISheet sheet, int rowCount, params string[] dataText)
        {
            if (dataText.IsNullOrEmpty()) return;

            sheet.CreateRow(rowCount);

            for (var i = 0; i < dataText.Length; i++)
                AddDateDetails(sheet, rowCount, i, dataText[i]);
        }

        //protected void AddArrayDetails(ISheet sheet, List<PriceListExportDto> data)
        //{
        //    var rowCount = 1;
        //    foreach (var master in data)
        //    {
        //        AddData(sheet, rowCount++, master.BranchName, master.PricingLevelName, master.FromDate, master.ToDate);
        //        AddData(sheet, rowCount++, " ", "Product Code", "Product Name", "Unit", "Sales Rate", "Min Qty",
        //            "Discount Percent", "Final Rate");
        //        foreach (var details in master.Details)
        //            AddData(sheet, rowCount++, " ", details.ProductCode, details.ProductName, details.UnitName,
        //                details.SalesRate.ToString(), details.MinQty.ToString(), details.DiscountPercent.ToString(),
        //                details.FinalRate.ToString());
        //    }
        //}

        protected void AddDateDetails(ISheet sheet, int rowIndex, int columnIndex, string headerText)
        {
            var cell = sheet.GetRow(rowIndex).CreateCell(columnIndex);
            cell.SetCellValue(headerText);
            var cellStyle = sheet.Workbook.CreateCellStyle();
            var font = sheet.Workbook.CreateFont();
            font.FontHeightInPoints = 10;
            cellStyle.SetFont(font);
            cell.CellStyle = cellStyle;
        }

        protected void AddHeaderWithMainHeader(ISheet sheet, int rowNumber, params string[] headerTexts)
        {
            if (headerTexts.IsNullOrEmpty()) return;

            sheet.CreateRow(rowNumber);

            for (var i = 0; i < headerTexts.Length; i++) AddHeaderWithMainHeader(sheet, rowNumber, i, headerTexts[i]);
        }

        protected void AddHeader2(ISheet sheet, string name, params string[] headerTexts)
        {
            if (headerTexts.IsNullOrEmpty()) return;
            sheet.CreateRow(0);
            var cell = sheet.GetRow(0).CreateCell(0);
            var cellRange = new CellRangeAddress(0, 0, 0, 15);
            sheet.AddMergedRegion(cellRange);
            var style = sheet.Workbook.CreateCellStyle();
            //style.WrapText = true;
            //style.ShrinkToFit = false;
            //style.BorderBottom = BorderStyle.Hair;
            //style.FillForegroundColor = HSSFColor.Blue.Index;
            var font = sheet.Workbook.CreateFont();
            font.IsBold = true;
            font.IsItalic = true;
            font.Color = 2;
            font.FontHeight = 400;

            style.SetFont(font);
            cell.CellStyle = style;

            cell.SetCellValue("Working details of " + name);

            sheet.CreateRow(1);
            sheet.AutoSizeColumn(0);

            for (var i = 0; i < headerTexts.Length; i++) AddHeader2(sheet, i, headerTexts[i]);
        }


        protected void AddHeaderSecoundWithMerge(ISheet sheet, int rowNumber, int columnStart, int columnEnd,
            string headerTexts)
        {
            if (headerTexts.IsNullOrWhiteSpace()) return;
            sheet.CreateRow(rowNumber);
            var cell = sheet.GetRow(rowNumber).CreateCell(columnStart);
            var cellRange = new CellRangeAddress(rowNumber, rowNumber, columnStart, columnEnd);
            sheet.AddMergedRegion(cellRange);
            var style = sheet.Workbook.CreateCellStyle();
            cell.SetCellValue(headerTexts);
        }

        protected void AddMasterHeader(ISheet sheet, int rowStart, int rowEnd, int columnStart, int columnEnd,
            string headerTexts, int fontSize = 12)
        {
            if (headerTexts.IsNullOrWhiteSpace()) return;
            if (sheet.LastRowNum != rowStart || sheet.LastRowNum == 0)
                sheet.CreateRow(rowStart);
            var cell = sheet.GetRow(rowStart).CreateCell(columnStart);
            var cellRange = new CellRangeAddress(rowStart, rowEnd, columnStart, columnEnd);
            sheet.AddMergedRegion(cellRange);
            var cellStyle = sheet.Workbook.CreateCellStyle();
            var font = sheet.Workbook.CreateFont();
            font.IsBold = true;
            font.FontHeightInPoints = fontSize;
            cellStyle.SetFont(font);
            cell.CellStyle = cellStyle;
            cell.SetCellValue(headerTexts);
        }

        protected void AddMasterHeaderNepali(ISheet sheet, int rowStart, int rowEnd, int columnStart, int columnEnd,
            string headerTexts, bool isNew, int fontSize = 12)
        {
            if (headerTexts.IsNullOrWhiteSpace()) return;
            if ((sheet.LastRowNum != rowStart || sheet.LastRowNum == 0) && isNew)
                sheet.CreateRow(rowStart);
            var cell = sheet.GetRow(rowStart).CreateCell(columnStart);
            var cellRange = new CellRangeAddress(rowStart, rowEnd, columnStart, columnEnd);
            sheet.AddMergedRegion(cellRange);
            var cellStyle = sheet.Workbook.CreateCellStyle();
            var font = sheet.Workbook.CreateFont();
            font.IsBold = true;
            font.FontHeightInPoints = fontSize;
            cellStyle.SetFont(font);
            cell.CellStyle = cellStyle;
            cell.SetCellValue(headerTexts);
        }

        protected void AddMasterHeader(ISheet sheet, int row, int column, string headerTexts, int fontSize = 12)
        {
            if (headerTexts.IsNullOrWhiteSpace()) return;
            if (sheet.LastRowNum < row)
                if (sheet.LastRowNum != row || sheet.LastRowNum == 0)
                    sheet.CreateRow(row);
            var cell = sheet.GetRow(row).CreateCell(column);
            var cellStyle = sheet.Workbook.CreateCellStyle();
            var font = sheet.Workbook.CreateFont();
            font.IsBold = true;
            //  font.Color = 14;          
            font.FontHeightInPoints = fontSize;
            cellStyle.SetFont(font);
            cell.CellStyle = cellStyle;
            cell.SetCellValue(headerTexts);
        }

        protected void AddHeader(ISheet sheet, int columnIndex, string headerText)
        {
            var cell = sheet.GetRow(0).CreateCell(columnIndex);
            cell.SetCellValue(headerText);
            var cellStyle = sheet.Workbook.CreateCellStyle();
            var font = sheet.Workbook.CreateFont();
            font.IsBold = true;
            font.FontHeightInPoints = 12;
            cellStyle.SetFont(font);
            cell.CellStyle = cellStyle;
        }

        protected void AddHeaderWithMainHeader(ISheet sheet, int rowNumber, int columnIndex, string headerText)
        {
            var cell = sheet.GetRow(rowNumber).CreateCell(columnIndex);
            cell.SetCellValue(headerText);
            var cellStyle = sheet.Workbook.CreateCellStyle();
            var font = sheet.Workbook.CreateFont();
            font.IsBold = true;
            font.FontHeightInPoints = 12;
            cellStyle.SetFont(font);
            cell.CellStyle = cellStyle;
        }

        protected void AddHeader2(ISheet sheet, int columnIndex, string headerText)
        {
            var cell = sheet.GetRow(1).CreateCell(columnIndex);
            cell.SetCellValue(headerText);
            var cellStyle = sheet.Workbook.CreateCellStyle();
            var font = sheet.Workbook.CreateFont();
            font.IsBold = true;
            font.FontHeightInPoints = 12;
            cellStyle.SetFont(font);
            cell.CellStyle = cellStyle;
        }

        protected void AddObjects<T>(ISheet sheet, IList<T> items, params Func<T, object>[] propertySelectors)
        {
            if (items.IsNullOrEmpty() || propertySelectors.IsNullOrEmpty()) return;

            for (var i = 1; i <= items.Count; i++)
            {
                var row = sheet.CreateRow(i);

                for (var j = 0; j < propertySelectors.Length; j++)
                {
                    var cell = row.CreateCell(j);
                    var value = propertySelectors[j](items[i - 1]);
                    if (value != null)
                    {
                        if (value is decimal or Guid)
                            cell.SetCellValue(Convert.ToDouble(value));
                        else
                            cell.SetCellValue(value.ToString());
                    }
                }
            }
        }


        //protected void AddObjectsWithArray(List<PartyWiseReportMaster> items, ISheet sheet)
        //{
        //    if (items.IsNullOrEmpty()) return;
        //    var rowNumber = 3;
        //    for (var i = 0; i < items.Count; i++)
        //    {
        //        var row = sheet.CreateRow(rowNumber);
        //        var cell1 = row.CreateCell(1);
        //        var val1 = items[i];
        //        var ledgerName = val1.LedgerName;
        //        var ledgerRow = rowNumber;
        //        if (val1 != null)
        //        {
        //            var cellStyle = sheet.Workbook.CreateCellStyle();
        //            cellStyle.BorderTop = BorderStyle.Thick;
        //            var font = sheet.Workbook.CreateFont();
        //            font.IsBold = true;
        //            font.FontHeightInPoints = 12;
        //            cellStyle.SetFont(font);
        //            cell1.CellStyle = cellStyle;
        //            cell1.SetCellValue(ledgerName);
        //        }

        //        var cellStyle1 = sheet.Workbook.CreateCellStyle();
        //        cellStyle1.BorderTop = BorderStyle.Thick;
        //        var cellStyle2 = sheet.Workbook.CreateCellStyle();
        //        cellStyle2.BorderTop = BorderStyle.Thin;
        //        for (var k = 0; k < val1.Details.Count; k++)
        //        {
        //            if (k != 0)
        //                row = sheet.CreateRow(rowNumber);
        //            var cell2 = row.CreateCell(2);
        //            var val2 = val1.Details[k];
        //            var product = val2.ProductName;
        //            var productRow = rowNumber;
        //            if (val2 != null)
        //            {
        //                var cellStyle = sheet.Workbook.CreateCellStyle();
        //                if (k == 0)
        //                    cellStyle.BorderTop = BorderStyle.Thick;
        //                var font = sheet.Workbook.CreateFont();
        //                font.IsBold = true;
        //                font.FontHeightInPoints = 11;
        //                cellStyle.SetFont(font);
        //                cell2.CellStyle = cellStyle;
        //                cell2.SetCellValue(product);
        //            }

        //            var inTransaction = 0;
        //            if (val2 != null)
        //                if (val2.Inward != null)
        //                    inTransaction = val2.Inward.Count;
        //            var outTransaction = 0;
        //            if (val2 != null)
        //                if (val2.Outward != null)
        //                    outTransaction = val2.Outward.Count;
        //            var maxTransaction = inTransaction > outTransaction ? inTransaction : outTransaction;
        //            for (var l = 0; l < maxTransaction; l++)
        //            {
        //                if (l != 0)
        //                    row = sheet.CreateRow(rowNumber);
        //                if (val2.Inward != null)
        //                    if (val2.Inward.Count > l)
        //                    {
        //                        var inWard = val2.Inward[l];
        //                        if (inWard != null)
        //                        {
        //                            var inVoucher = inWard.VoucherTypeName;
        //                            var cell3 = row.CreateCell(3);
        //                            if (inVoucher != null)
        //                            {
        //                                if (l == 0 && k == 0)
        //                                    cell3.CellStyle = cellStyle1;
        //                                if (l == 0 && k != 0)
        //                                    cell3.CellStyle = cellStyle2;
        //                                cell3.SetCellValue(inVoucher);
        //                            }

        //                            var date = inWard.Date;
        //                            var cell8 = row.CreateCell(4);
        //                            if (date != null)
        //                            {
        //                                if (l == 0 && k == 0)
        //                                    cell8.CellStyle = cellStyle1;
        //                                if (l == 0 && k != 0)
        //                                    cell8.CellStyle = cellStyle2;
        //                                cell8.SetCellValue(date);
        //                            }

        //                            var inQty = inWard.Quantity;
        //                            var cell4 = row.CreateCell(5);
        //                            if (l == 0 && k == 0)
        //                                cell4.CellStyle = cellStyle1;
        //                            if (l == 0 && k != 0)
        //                                cell4.CellStyle = cellStyle2;
        //                            cell4.SetCellValue(Convert.ToDouble(inQty));

        //                            var inRate = inWard.Rate;
        //                            var cell5 = row.CreateCell(6);
        //                            if (l == 0 && k == 0)
        //                                cell5.CellStyle = cellStyle1;
        //                            if (l == 0 && k != 0)
        //                                cell5.CellStyle = cellStyle2;
        //                            cell5.SetCellValue(Convert.ToDouble(inRate));

        //                            var inTax = inWard.TaxAmount;
        //                            var cell6 = row.CreateCell(7);
        //                            if (l == 0 && k == 0)
        //                                cell6.CellStyle = cellStyle1;
        //                            if (l == 0 && k != 0)
        //                                cell6.CellStyle = cellStyle2;
        //                            cell6.SetCellValue(Convert.ToDouble(inTax));

        //                            var inAmount = inWard.Amount;
        //                            var cell7 = row.CreateCell(8);
        //                            if (l == 0 && k == 0)
        //                                cell7.CellStyle = cellStyle1;
        //                            if (l == 0 && k != 0)
        //                                cell7.CellStyle = cellStyle2;
        //                            cell7.SetCellValue(Convert.ToDouble(inAmount));
        //                        }
        //                    }

        //                if (val2.Outward != null)
        //                    if (val2.Outward.Count > l)
        //                    {
        //                        var outWard = val2.Outward[l];
        //                        if (outWard != null)
        //                        {
        //                            var outVoucher = outWard.VoucherTypeName;
        //                            var cell3 = row.CreateCell(9);
        //                            if (outVoucher != null)
        //                            {
        //                                if (l == 0 && k == 0)
        //                                    cell3.CellStyle = cellStyle1;
        //                                if (l == 0 && k != 0)
        //                                    cell3.CellStyle = cellStyle2;
        //                                cell3.SetCellValue(outVoucher);
        //                            }

        //                            var date = outWard.Date;
        //                            var cell8 = row.CreateCell(10);
        //                            if (date != null)
        //                            {
        //                                if (l == 0 && k == 0)
        //                                    cell8.CellStyle = cellStyle1;
        //                                if (l == 0 && k != 0)
        //                                    cell8.CellStyle = cellStyle2;
        //                                cell8.SetCellValue(date);
        //                            }

        //                            var outQty = outWard.Quantity;
        //                            var cell4 = row.CreateCell(11);
        //                            if (l == 0 && k == 0)
        //                                cell4.CellStyle = cellStyle1;
        //                            if (l == 0 && k != 0)
        //                                cell4.CellStyle = cellStyle2;
        //                            cell4.SetCellValue(Convert.ToDouble(outQty));

        //                            var outRate = outWard.Rate;
        //                            var cell5 = row.CreateCell(12);
        //                            if (l == 0 && k == 0)
        //                                cell5.CellStyle = cellStyle1;
        //                            if (l == 0 && k != 0)
        //                                cell5.CellStyle = cellStyle2;
        //                            cell5.SetCellValue(Convert.ToDouble(outRate));

        //                            var outTax = outWard.TaxAmount;
        //                            var cell6 = row.CreateCell(13);
        //                            if (l == 0 && k == 0)
        //                                cell6.CellStyle = cellStyle1;
        //                            if (l == 0 && k != 0)
        //                                cell6.CellStyle = cellStyle2;
        //                            cell6.SetCellValue(Convert.ToDouble(outTax));

        //                            var outAmount = outWard.Amount;
        //                            var cell7 = row.CreateCell(14);
        //                            if (l == 0 && k == 0)
        //                                cell7.CellStyle = cellStyle1;
        //                            if (l == 0 && k != 0)
        //                                cell7.CellStyle = cellStyle2;
        //                            cell7.SetCellValue(Convert.ToDouble(outAmount));
        //                        }
        //                    }

        //                if (l == 0)
        //                {
        //                    var closing = val2.Closing;
        //                    if (closing != null)
        //                    {
        //                        var outQty = closing.Quantity;
        //                        var cell4 = row.CreateCell(15);
        //                        if (k == 0)
        //                            cell4.CellStyle = cellStyle1;
        //                        if (k != 0)
        //                            cell4.CellStyle = cellStyle2;
        //                        cell4.SetCellValue(Convert.ToDouble(outQty));

        //                        var outRate = closing.Rate;
        //                        var cell5 = row.CreateCell(16);
        //                        if (k == 0)
        //                            cell5.CellStyle = cellStyle1;
        //                        if (l == 0 && k != 0)
        //                            cell5.CellStyle = cellStyle2;
        //                        cell5.SetCellValue(Convert.ToDouble(outRate));

        //                        var outTax = closing.TaxAmount;
        //                        var cell6 = row.CreateCell(17);
        //                        if (outTax != null)
        //                        {
        //                            if (l == 0 && k == 0)
        //                                cell6.CellStyle = cellStyle1;
        //                            if (l == 0 && k != 0)
        //                                cell6.CellStyle = cellStyle2;
        //                            cell6.SetCellValue(Convert.ToDouble(outTax));
        //                        }

        //                        var outAmount = closing.Amount;
        //                        var cell7 = row.CreateCell(18);
        //                        if (outAmount != null)
        //                        {
        //                            if (l == 0 && k == 0)
        //                                cell7.CellStyle = cellStyle1;
        //                            if (l == 0 && k != 0)
        //                                cell7.CellStyle = cellStyle2;
        //                            cell7.SetCellValue(Convert.ToDouble(outAmount));
        //                        }
        //                    }
        //                }

        //                rowNumber++;
        //            }

        //            if (rowNumber > productRow + 1)
        //            {
        //                var cellRange = new CellRangeAddress(productRow, rowNumber - 1, 2, 2);
        //                sheet.AddMergedRegion(cellRange);
        //            }
        //        }

        //        if (rowNumber > ledgerRow + 1)
        //        {
        //            var cellRange1 = new CellRangeAddress(ledgerRow, rowNumber - 1, 1, 1);
        //            sheet.AddMergedRegion(cellRange1);
        //        }
        //    }
        //}

        protected void AddObjectsWithDetails<T>(ISheet sheet, int startRow, IList<T> items,
            params Func<T, object>[] propertySelectors)
        {
            if (items.IsNullOrEmpty() || propertySelectors.IsNullOrEmpty()) return;
            var rowCount = startRow;
            for (var i = 1; i <= items.Count; i++)
            {
                var row = sheet.CreateRow(rowCount++);

                for (var j = 0; j < 11; j++)
                {
                    var cell = row.CreateCell(j);
                    var value = propertySelectors[j](items[i - 1]);
                    if (value != null)
                    {
                        if (value.GetType() == typeof(decimal) || value.GetType() == typeof(Guid))
                            cell.SetCellValue(Convert.ToDouble(value));
                        else
                            cell.SetCellValue(value.ToString());
                    }
                }

                var value1 = propertySelectors[11](items[i - 1]);
                var myList = value1 as IEnumerable<ProductDetailsForExcelExport>;
                if (myList != null)
                    foreach (var single in myList)
                    {
                        var row1 = sheet.CreateRow(rowCount++);
                        var cell = row1.CreateCell(2);
                        cell.SetCellValue(single.Name);
                        var cell1 = row1.CreateCell(3);
                        cell1.SetCellValue(Convert.ToDouble(single.Qty));
                        var cell2 = row1.CreateCell(4);
                        cell2.SetCellValue(single.Unit);
                        var cell3 = row1.CreateCell(5);
                        cell3.SetCellValue(Convert.ToDouble(single.Rate));
                        var cell4 = row1.CreateCell(6);
                        cell4.SetCellValue(Convert.ToDouble(single.Amount));
                    }
            }
        }


        protected void AddObjectsNepali<T>(ISheet sheet, int startRow, IList<T> items,
            params Func<T, object>[] propertySelectors)
        {
            if (items.IsNullOrEmpty() || propertySelectors.IsNullOrEmpty()) return;

            for (var i = startRow; i <= items.Count + startRow - 1; i++)
            {
                var row = sheet.CreateRow(i);

                for (var j = 0; j < propertySelectors.Length; j++)
                {
                    var cell = row.CreateCell(j);
                    var value = propertySelectors[j](items[i - 4]);
                    if (value != null)
                    {
                        if (value.GetType() == typeof(decimal) || value.GetType() == typeof(Guid) ||
                            value.GetType() == typeof(int))
                            cell.SetCellValue(Convert.ToDouble(value));
                        else
                            cell.SetCellValue(value.ToString());
                    }
                }
            }
        }

        protected void AddObjectsWithMainHeader<T>(ISheet sheet, IList<T> items, int rowNumber,
            params Func<T, object?>[] propertySelectors)
        {
            if (items.IsNullOrEmpty() || propertySelectors.IsNullOrEmpty()) return;

            for (var i = rowNumber; i < items.Count + rowNumber; i++)
            {
                var row = sheet.CreateRow(i);

                for (var j = 0; j < propertySelectors.Length; j++)
                {
                    var cell = row.CreateCell(j);
                    var value = propertySelectors[j](items[i - rowNumber]);
                    if (value != null)
                    {
                        if (value.GetType() == typeof(decimal) || value.GetType() == typeof(Guid))
                            cell.SetCellValue(Convert.ToDouble(value));
                        else
                            cell.SetCellValue(value.ToString());
                    }
                }
            }
        }

        protected void AddObjectsOfDayBook<T>(ISheet sheet, IList<T> items, int rowNumber, int colStart = 0,
            params Func<T, object>[] propertySelectors)
        {
            if (items.IsNullOrEmpty() || propertySelectors.IsNullOrEmpty()) return;

            for (var i = rowNumber; i < items.Count + rowNumber; i++)
            {
                IRow row;
                if (sheet.LastRowNum < i)
                    row = sheet.CreateRow(i);
                else
                    row = sheet.GetRow(i);

                for (var j = 0; j < propertySelectors.Length; j++)
                {
                    var cell = row.CreateCell(j + colStart);
                    var value = propertySelectors[j](items[i - rowNumber]);
                    if (value != null)
                    {
                        if (value.GetType() == typeof(decimal) || value.GetType() == typeof(Guid) ||
                            value.GetType() == typeof(int) || value.GetType() == typeof(double) ||
                            value.GetType() == typeof(long))
                            cell.SetCellValue(Convert.ToDouble(value));
                        else
                            cell.SetCellValue(value.ToString());
                    }
                }
            }
        }


        //protected void AddObjectArray(ISheet sheet, List<SpareReportDto> items)
        //{
        //    if (items.IsNullOrEmpty()) return;
        //    var updatedi = 0;
        //    for (var i = 0; i < items.Count; i++)
        //    {
        //        var value = items[i];
        //        var row = sheet.CreateRow(updatedi + 1);
        //        var cell = row.CreateCell(0);
        //        cell.SetCellValue(value.Sn);
        //        var cell1 = row.CreateCell(1);
        //        cell1.SetCellValue(value.IncomingDate);
        //        var cell2 = row.CreateCell(2);
        //        cell2.SetCellValue(value.PpNo);
        //        var cell3 = row.CreateCell(3);
        //        cell3.SetCellValue(value.Product);
        //        var cell4 = row.CreateCell(4);
        //        cell4.SetCellValue(value.ModelNo);
        //        var cell5 = row.CreateCell(5);
        //        cell5.SetCellValue(value.Spare);
        //        var cell6 = row.CreateCell(6);
        //        cell6.SetCellValue(value.Condition);
        //        updatedi += 1;
        //        var detailsOnly = value.Details;
        //        for (var m = 0; m < detailsOnly.Count; m++)
        //        {
        //            var detailOnly = detailsOnly[m];
        //            var row1 = sheet.CreateRow(updatedi + 1);
        //            var cell7 = row1.CreateCell(7);
        //            cell7.SetCellValue(detailOnly.Part);
        //            var cell8 = row1.CreateCell(8);
        //            cell8.SetCellValue(detailOnly.Spare);
        //            var cell10 = row1.CreateCell(10);
        //            cell10.SetCellValue(detailOnly.SerialNo);
        //            var cell11 = row1.CreateCell(11);
        //            cell11.SetCellValue(detailOnly.Qty);
        //            updatedi = updatedi + 1;
        //        }
        //    }
        //}

        protected void AddObjects2<T>(ISheet sheet, IList<T> items, params Func<T, object>[] propertySelectors)
        {
            if (items.IsNullOrEmpty() || propertySelectors.IsNullOrEmpty()) return;
            var itemCount = items.Count + 1;
            try
            {
                for (var i = 2; i <= itemCount; i++)
                {
                    var row = sheet.CreateRow(i);

                    for (var j = 0; j < propertySelectors.Length; j++)
                    {
                        var cell = row.CreateCell(j);
                        var value = propertySelectors[j](items[i - 2]);
                        if (value != null)
                        {
                            if (value.GetType() == typeof(decimal) || value.GetType() == typeof(int))
                                cell.SetCellValue(Convert.ToDouble(value));
                            else
                                cell.SetCellValue(value.ToString());
                        }
                    }
                }
            }
            catch (IndexOutOfRangeException ex)
            {
                var a = 8;
                throw new Exception(ex.Message);
            }
        }

        protected void ColumnResize(ISheet sheet, int number, int start = 1)
        {
            for (var i = start; i <= number; i++)
                sheet.AutoSizeColumn(i);
        }

        public void ColumnResizeValue(ISheet sheet, int col)
        {
            for (var i = 0; i < col; i++)
            {
                if (i == 3)
                    continue;
                sheet.SetColumnWidth(i, 4000);
            }
        }

        protected void AddTailer(ISheet sheet, int startCol, int rowIndex, params decimal[] tailerAmount)
        {
            var row = sheet.CreateRow(rowIndex + 1);
            for (var i = 0; i < tailerAmount.Length; i++)
            {
                var cell = sheet.GetRow(rowIndex + 1).CreateCell(startCol + i);
                cell.SetCellValue((double)tailerAmount[i]);

                var cellStyle = sheet.Workbook.CreateCellStyle();
                var font = sheet.Workbook.CreateFont();
                font.IsBold = true;
                font.FontHeightInPoints = 12;
                cellStyle.SetFont(font);
                cell.CellStyle = cellStyle;
            }
        }

        protected void AddHeaderSwastik(ISheet sheet, int rowIndex, params string[] headerTexts)
        {
            if (headerTexts.IsNullOrEmpty()) return;

            for (var i = 0; i < rowIndex + 1; i++) sheet.CreateRow(i);
            for (var i = 0; i < headerTexts.Length; i++) AddHeaderSwastik(sheet, rowIndex, i, headerTexts[i]);
        }

        protected void AddHeaderSwastik(ISheet sheet, int rowIndex, int columnIndex, string headerText)
        {
            var cell = sheet.GetRow(rowIndex).CreateCell(columnIndex);
            cell.SetCellValue(headerText);
            var cellStyle = sheet.Workbook.CreateCellStyle();
            var font = sheet.Workbook.CreateFont();
            font.IsBold = true;
            font.FontHeightInPoints = 12;
            cellStyle.SetFont(font);
            cell.CellStyle = cellStyle;
        }

        protected void AddObjectsSwastik<T>(ISheet sheet, int startRowIndex, IList<T> items,
            params Func<T, object>[] propertySelectors)
        {
            if (items.IsNullOrEmpty() || propertySelectors.IsNullOrEmpty()) return;

            var m = items.Count + startRowIndex - 1;
            for (var i = startRowIndex; i <= m; i++)
            {
                var row = sheet.CreateRow(i);

                for (var j = 0; j < propertySelectors.Length; j++)
                {
                    var cell = row.CreateCell(j);
                    var value = propertySelectors[j](items[i - startRowIndex]);
                    if (value != null)
                        if (value.GetType() == typeof(string))
                        {
                            cell.SetCellValue(value.ToString());
                            cell.CellStyle.ShrinkToFit = true;
                        }
                        else
                        {
                            cell.SetCellValue(Convert.ToDouble(value));
                            cell.CellStyle.ShrinkToFit = true;
                        }
                }
            }
        }

        protected virtual void Save(XSSFWorkbook excelPackage, FileDto file)
        {
            using (var stream = new MemoryStream())
            {
                excelPackage.Write(stream);
                tempFileCacheManager.SetFile(file.FileToken, stream.ToArray());
            }
        }

        protected void SetCellDataFormat(ICell cell, string dataFormat)
        {
            if (cell == null)
                return;

            var dateStyle = GetDateCellStyle(cell, dataFormat);
            var format = GetDateDataFormat(cell, dataFormat);

            dateStyle.DataFormat = format.GetFormat(dataFormat);
            cell.CellStyle = dateStyle;
            if (DateTime.TryParse(cell.StringCellValue, out var datetime))
                cell.SetCellValue(datetime);
        }
    }
}
