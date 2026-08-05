using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Accounting;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.Enums;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Inventory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextWave.Erp.MultiTenancy
{
    public class DbMigrations(
         IRepository<VoucherType, Guid> voucherTypeRepository,
         IRepository<Branch, Guid> branchRepository,
         IRepository<AccountGroup, Guid> accountGroupRepository,
         IRepository<AccountLedger, Guid> accountLedgerRepository,
         IRepository<Unit, Guid> unitRepository,
         IRepository<VoucherNumbering, Guid> voucherNumberingRepository,
         IRepository<ProductGroup, Guid> productGroupRepository,
         IRepository<Tax, Guid> taxRepository,
         IRepository<FinancialYear, Guid> FinancialYearRepository,
         IRepository<FinancialYearSelect,Guid> financialYearSelectRepository)
         : ErpDomainServiceBase
    {
        //public byte[] LogoImg()
        //{
        //    byte[] img =
        //    [
        //        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3f, 0x80, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00,
        //        0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1f, 0x00, 0x00, 0x00, 0x00, 0x00,
        //        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
        //        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        //        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        //        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00,
        //        0x0f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00,
        //        0x0f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xc0,
        //        0x07, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xe0, 0x00, 0x00, 0x07, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff,
        //        0xff, 0xfe, 0x00, 0x00, 0x3e, 0x00, 0x3f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff,
        //        0xff, 0xff, 0xff, 0xf8, 0x00, 0x7f, 0xff, 0xff, 0x07, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00,
        //        0x0f, 0xff, 0xff, 0xff, 0xff, 0xc0, 0x07, 0xff, 0xff, 0xff, 0xf8, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xff, 0x20, 0x1f, 0xff, 0xff, 0xff, 0xfe, 0x1f, 0xff, 0xff,
        //        0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xfe, 0x40, 0x7f, 0xff, 0xff, 0xff, 0xff, 0xe7,
        //        0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xfb, 0x83, 0xff, 0xff, 0x80, 0xff,
        //        0xff, 0xf9, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xe4, 0x83, 0xff, 0xfe,
        //        0x00, 0x7f, 0xff, 0xfe, 0x3f, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xff, 0xdf,
        //        0xff, 0xfc, 0x00, 0x3f, 0xff, 0xff, 0xcf, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff,
        //        0xff, 0xdf, 0xff, 0xfe, 0x00, 0x1f, 0xff, 0xff, 0xf7, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff,
        //        0xff, 0xff, 0xfe, 0xbf, 0xff, 0xfc, 0x00, 0x1f, 0xff, 0xff, 0xfd, 0xff, 0xff, 0xff, 0xf0, 0x00,
        //        0x0f, 0xff, 0xff, 0xff, 0xff, 0x7f, 0xff, 0xfe, 0x00, 0x0f, 0xff, 0xff, 0xfe, 0x7f, 0xff, 0xff,
        //        0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xfe, 0xff, 0xff, 0xfc, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xbf,
        //        0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xfc, 0xff, 0xff, 0xfc, 0x00, 0x0f, 0xff, 0xff,
        //        0xff, 0xcf, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xfd, 0xff, 0xff, 0xfe, 0x00, 0x0f,
        //        0xff, 0xff, 0xff, 0xf7, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xfd, 0xff, 0xff, 0xfa,
        //        0x00, 0x1f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xfc, 0x00, 0x1f, 0xff, 0xff, 0xff, 0xfe, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xfe, 0x00, 0x3f, 0xff, 0xff, 0xff, 0xff, 0x7f, 0xff, 0xf0, 0x00, 0x0f, 0xff,
        //        0xff, 0xff, 0xfb, 0xff, 0xff, 0xff, 0x00, 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00,
        //        0x2f, 0xff, 0xff, 0xff, 0xfb, 0xff, 0xff, 0xff, 0x81, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xf0, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xf0, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xfc,
        //        0x00, 0x1f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xfc, 0x00, 0x1f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x2f, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xfc, 0x00, 0x1e, 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff,
        //        0xff, 0xff, 0xfb, 0xff, 0xff, 0xfc, 0x00, 0x1e, 0x3f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00,
        //        0x0f, 0xff, 0xff, 0xff, 0xfd, 0xff, 0xff, 0xfc, 0x00, 0x1c, 0x0f, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xfc, 0x00, 0x1c, 0x07, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xfe, 0xff, 0xff, 0xfc, 0x00, 0x1c, 0x01, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xfd, 0xff, 0xff, 0xfc, 0x00, 0x1c,
        //        0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xfd, 0xff, 0xff, 0xfc,
        //        0x00, 0x1c, 0x00, 0x7f, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xfd, 0x7f,
        //        0xff, 0xfc, 0x00, 0x1c, 0x00, 0x1f, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff,
        //        0xfd, 0x7f, 0xff, 0xfc, 0x00, 0x1c, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff,
        //        0xff, 0xff, 0xff, 0xbf, 0xff, 0xfc, 0x00, 0x1c, 0x00, 0x07, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00,
        //        0x0f, 0xff, 0xff, 0xff, 0xff, 0x9f, 0xff, 0xfc, 0x00, 0x1c, 0x00, 0x03, 0xff, 0xff, 0xff, 0xff,
        //        0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xfe, 0x5f, 0xff, 0xfc, 0x00, 0x1c, 0x00, 0x01, 0xff, 0xff,
        //        0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xfc, 0x2f, 0xff, 0xfc, 0x00, 0x1c, 0x00, 0x00,
        //        0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xfc, 0x4f, 0xff, 0xfc, 0x00, 0x1c,
        //        0x00, 0x00, 0x7f, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xfc, 0x57, 0xff, 0xfc,
        //        0x00, 0x1c, 0x00, 0x00, 0x3f, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xfc, 0x2b,
        //        0xff, 0xfc, 0x00, 0x1c, 0x00, 0x00, 0x1f, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff,
        //        0xfe, 0x21, 0xff, 0xfc, 0x00, 0x1c, 0x00, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff,
        //        0xff, 0xff, 0xff, 0x00, 0xff, 0xfc, 0x00, 0x1e, 0x00, 0x00, 0x07, 0xff, 0xff, 0xff, 0xf0, 0x00,
        //        0x0f, 0xff, 0xff, 0xff, 0xff, 0x80, 0x7f, 0xfc, 0x00, 0x1e, 0x00, 0x00, 0x03, 0xff, 0xff, 0xff,
        //        0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xff, 0xc0, 0x3f, 0xfc, 0x00, 0x1f, 0x00, 0x00, 0x03, 0xff,
        //        0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xfd, 0xc0, 0x1f, 0xfc, 0x00, 0x1f, 0x80, 0x00,
        //        0x01, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xbe, 0x00, 0x0f, 0xfc, 0x00, 0x1f,
        //        0xc0, 0x00, 0x00, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xf6, 0x00, 0x07, 0xfc,
        //        0x00, 0x1f, 0xe0, 0x00, 0x00, 0x7f, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xdc, 0x00,
        //        0x03, 0xfc, 0x00, 0x1f, 0xf0, 0x00, 0x00, 0x7f, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff,
        //        0xf6, 0x00, 0x01, 0xfc, 0x00, 0x1f, 0xf8, 0x00, 0x00, 0x3f, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff,
        //        0xff, 0xff, 0xf7, 0x80, 0x00, 0xfc, 0x00, 0x1f, 0xfc, 0x00, 0x00, 0x3f, 0xff, 0xff, 0xf0, 0x00,
        //        0x0f, 0xff, 0xff, 0xff, 0xff, 0x80, 0x00, 0x7c, 0x00, 0x1f, 0xfe, 0x00, 0x00, 0x1f, 0xff, 0xff,
        //        0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xfd, 0x80, 0x00, 0x1c, 0x00, 0x1f, 0xff, 0x00, 0x00, 0x0f,
        //        0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xfe, 0xc0, 0x00, 0x1c, 0x00, 0x1f, 0xff, 0x80,
        //        0x00, 0x0f, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xfe, 0xc0, 0x00, 0x00, 0x00, 0x1f,
        //        0xff, 0xc0, 0x00, 0x07, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xfe, 0x80, 0x00, 0x00,
        //        0x00, 0x1f, 0xff, 0xe0, 0x00, 0x07, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xff, 0x80,
        //        0x00, 0x00, 0x00, 0x1f, 0xff, 0xe0, 0x00, 0x03, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff, 0xff, 0xff,
        //        0xff, 0x90, 0x00, 0x00, 0x00, 0x1f, 0xff, 0xf0, 0x00, 0x03, 0xff, 0xff, 0xf0, 0x00, 0x0f, 0xff,
        //        0xff, 0xff, 0xff, 0xc0, 0x00, 0x00, 0x00, 0x1f, 0xff, 0xf8, 0x00, 0x03, 0xff, 0xff, 0xf8, 0x00,
        //        0x0f, 0xff, 0xff, 0xff, 0xff, 0xe0, 0x00, 0x00, 0x00, 0x1f, 0xff, 0xf8, 0x00, 0x01, 0xff, 0xff,
        //        0xfc, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x00, 0x00, 0x00, 0x1f, 0xff, 0xfc, 0x00, 0x01,
        //        0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf8, 0x00, 0x00, 0x00, 0x1f, 0xff, 0xfe,
        //        0x00, 0x01, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xfe, 0x00, 0x00, 0x00, 0x1f,
        //        0xff, 0xfe, 0x00, 0x00, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00,
        //        0x00, 0x1f, 0xff, 0xff, 0x00, 0x00, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0x80, 0x00, 0x00, 0x1f, 0xff, 0xff, 0x00, 0x00, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xc0, 0x00, 0x00, 0x1f, 0xff, 0xff, 0x80, 0x00, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xe0, 0x00, 0x00, 0x1f, 0xff, 0xff, 0x80, 0x00, 0xff, 0xff, 0xff, 0xc0,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf8, 0x00, 0x00, 0x1f, 0xff, 0xff, 0x80, 0x00, 0xff, 0xff,
        //        0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xfc, 0x00, 0x00, 0x1f, 0xff, 0xff, 0xc0, 0x00,
        //        0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x1f, 0xff, 0xff,
        //        0xc0, 0x00, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x80, 0x00, 0x1f,
        //        0xff, 0xff, 0xc0, 0x00, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xe0,
        //        0x00, 0x1f, 0xff, 0xff, 0xc0, 0x00, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xf8, 0x00, 0x1f, 0xff, 0xff, 0xc0, 0x00, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xfc, 0x00, 0x1f, 0xff, 0xff, 0xc0, 0x00, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x80, 0x1f, 0xff, 0xff, 0xc0, 0x00, 0xff, 0xff, 0xff, 0xc0,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x1f, 0xff, 0xff, 0xc0, 0x00, 0xff, 0xff,
        //        0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xfc, 0x1f, 0xff, 0xff, 0xc0, 0x01,
        //        0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xc0, 0x01, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xbf, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xc0, 0x01, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xdf, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0x80, 0x03, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xe7, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x80, 0x07, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xf3,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x80, 0x07, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff,
        //        0xff, 0xfc, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x0f, 0xff, 0xff, 0xff, 0xc0,
        //        0xff, 0xff, 0xff, 0xfe, 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xfe, 0x00, 0x0f, 0xff, 0xff,
        //        0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0x9f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xfc, 0x00, 0x1f,
        //        0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xc7, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf8,
        //        0x00, 0x3f, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xf1, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xf0, 0x00, 0x7f, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xfc, 0x7f, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xe0, 0x00, 0xff, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0x1f,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xc0, 0x03, 0xff, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xc3, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x07, 0xff, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xf0, 0xff, 0xff, 0xff, 0xff, 0xfc, 0x00, 0x1f, 0xff, 0xff, 0xff, 0xff, 0xc0,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xfe, 0x1f, 0xff, 0xff, 0xff, 0xe0, 0x00, 0x7f, 0xff, 0xff, 0xff,
        //        0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x81, 0xff, 0xff, 0xff, 0x00, 0x01, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0, 0x07, 0xff, 0xc0, 0x00, 0x07,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00,
        //        0x00, 0x3f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf0,
        //        0x00, 0x00, 0x03, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xc0, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xc0,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xc0,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xc0, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xc0,
        //        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xc0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //        0xff, 0xff, 0xff, 0xc0
        //    ];
        //    return img;
        //}



        [UnitOfWork]
        public async Task<Guid> CreateCompany(int id, string talentName,long adminUserId)
        {
            var tenantId = id;
            var branch = new Branch
            {
                Id = Guid.Empty,
                Email = "",
                Name = talentName,
                CompanyName = talentName,
                BranchCode = "",
                Address = "",
                PhoneNo1 = "",
                PhoneNo2 = "",
                Description = "",
                Status = true,
                BranchType = BranchType.Shop,
                BranchId = null,
                TenantId = tenantId,
                IsMain = true,
                Image1 = null,
                PANumber = "",
                State = StateEnum.Koshi
            };

            var branchId = await branchRepository.InsertAndGetIdAsync(branch);
            var financialYear = new FinancialYear
            {
                Name = "2082⟶83",
                FromDate = DateConverter.ConvertToEnglish("2082/04/01"),
                ToDate = DateConverter.ConvertToEnglish("2083/03/32"),
                FromMiti = "2082/04/01",
                ToMiti = "2083/03/32",
                Status = true,
                TenantId = tenantId
            };
            var financialYearId = await FinancialYearRepository.InsertAndGetIdAsync(financialYear);

            await financialYearSelectRepository.InsertAsync(new FinancialYearSelect
            {
                FinancialYearId = financialYearId,
                TenantId = tenantId,
                Date = DateTime.Today,
                UserId = adminUserId
            });

            await InitialInventory(branchId, tenantId);
            await GetInitialAccountGroups(branchId, tenantId);
            foreach (var objVoucherType in GetInitialvoucherType(tenantId, branchId))
                await AddVoucherTypeIfNotExistsAsync(objVoucherType, financialYearId, tenantId);

            return branchId;
        }


        //end company
        private async Task GetInitialAccountGroups(Guid branchId, int tenantId)
        {

            var primary = new AccountGroup
            {
                TenantId = tenantId,
                Name = "Primary",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Na,
                GroupUnder = null
            };
            var agPrimaryId = await accountGroupRepository.InsertAndGetIdAsync(primary);

            var agCapital = await accountGroupRepository.InsertAndGetIdAsync(new AccountGroup
            {
                TenantId = tenantId,
                Name = "Capital Account",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Liabilities,
                GroupUnder = agPrimaryId
            });

            var loanAndLiabilities = new AccountGroup
            {
                TenantId = tenantId,
                Name = "Loans (Liability)",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Liabilities,
                GroupUnder = agPrimaryId
            };
            var agLoanAndLiabitiesId = await accountGroupRepository.InsertAndGetIdAsync(loanAndLiabilities);

            var currentLiabilities = new AccountGroup
            {
                TenantId = tenantId,
                Name = "Current Liabilities",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Liabilities,
                GroupUnder = agPrimaryId
            };
            var agCurrentLiabilitiesId = await accountGroupRepository.InsertAndGetIdAsync(currentLiabilities);

            var currentAssets = new AccountGroup
            {
                TenantId = tenantId,
                Name = "Current Assets",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Assets,
                GroupUnder = agPrimaryId
            };
            var agCurrentAssetsId = await accountGroupRepository.InsertAndGetIdAsync(currentAssets);

            await accountGroupRepository.InsertAsync(new AccountGroup
            {
                TenantId = tenantId,
                Name = "Investments",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Assets,
                GroupUnder = agPrimaryId
            });

            var fixedassetsId = await accountGroupRepository.InsertAndGetIdAsync(new AccountGroup
            {
                TenantId = tenantId,
                Name = "Fixed Assets",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Assets,
                GroupUnder = agPrimaryId
            });

            await accountGroupRepository.InsertAndGetIdAsync(new AccountGroup
            {
                TenantId = tenantId,
                Name = "Misc Assets",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Assets,
                GroupUnder = fixedassetsId
            });

            var salesAccount = new AccountGroup
            {
                TenantId = tenantId,
                Name = "Sales Account",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = true,
                Nature = AccountGroupNature.Income,
                GroupUnder = agPrimaryId
            };
            var agSalesAccountId = await accountGroupRepository.InsertAndGetIdAsync(salesAccount);

            var purchaseAccount = new AccountGroup
            {
                TenantId = tenantId,
                Name = "Purchase Account",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = true,
                Nature = AccountGroupNature.Expenses,
                GroupUnder = agPrimaryId
            };
            var agPurchaseAccountId = await accountGroupRepository.InsertAndGetIdAsync(purchaseAccount);

            await accountLedgerRepository.InsertAsync(new AccountLedger
            {
                TenantId = tenantId,
                ParentId = null,
                Name = "PDC Payable",
                Narration = " ",
                Address = " ",
                Phone = " ",
                Email = " ",
                CreditPeriod = 0,
                Pan = " ",
                Status = true,
                IsDefault = true,
                CrOrDr = DrOrCr.Cr,
                AccountGroupId = agCurrentLiabilitiesId
            });

            await accountLedgerRepository.InsertAsync(new AccountLedger
            {
                TenantId = tenantId,
                ParentId = null,
                Name = "PDC Receivable",
                Narration = " ",
                Address = " ",
                Phone = " ",
                Email = " ",
                CreditPeriod = 0,
                Pan = " ",
                Status = true,
                IsDefault = true,
                CrOrDr = DrOrCr.Dr,
                AccountGroupId = agCurrentAssetsId
            });

            await accountGroupRepository.InsertAsync(new AccountGroup
            {
                TenantId = tenantId,
                Name = "Branch / Divisions",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Liabilities,
                GroupUnder = agPrimaryId
            });

            await accountGroupRepository.InsertAsync(new AccountGroup
            {
                TenantId = tenantId,
                Name = "Misc.Expenses (Assets)",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Assets,
                GroupUnder = agPrimaryId
            });

            await accountGroupRepository.InsertAsync(new AccountGroup
            {
                TenantId = tenantId,
                Name = "Suspense A/C",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Liabilities,
                GroupUnder = agPrimaryId
            });

            await accountLedgerRepository.InsertAsync(new AccountLedger
            {
                TenantId = tenantId,
                Name = "Sales Account",
                Narration = " ",
                Address = " ",
                Phone = " ",
                Email = " ",
                CreditPeriod = 0,
                Pan = " ",
                Status = true,
                IsDefault = true,
                CrOrDr = DrOrCr.Dr,
                AccountGroupId = agSalesAccountId
            });


            await accountLedgerRepository.InsertAsync(new AccountLedger
            {
                TenantId = tenantId,
                Name = "Purchase Account",
                Narration = " ",
                Address = " ",
                Phone = " ",
                Email = " ",
                CreditPeriod = 0,
                Pan = " ",
                Status = true,
                IsDefault = true,
                CrOrDr = DrOrCr.Dr,
                AccountGroupId = agPurchaseAccountId
            });


            var directIncome = new AccountGroup
            {
                TenantId = tenantId,
                Name = "Direct Income",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = true,
                Nature = AccountGroupNature.Income,
                GroupUnder = agPrimaryId
            };
            var agDirectIncome = await accountGroupRepository.InsertAndGetIdAsync(directIncome);


            var directExpenses = new AccountGroup
            {
                TenantId = tenantId,
                Name = "Direct Expenses",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = true,
                Nature = AccountGroupNature.Expenses,
                GroupUnder = agPrimaryId
            };
            await accountGroupRepository.InsertAsync(directExpenses);


            var indirectIncome = new AccountGroup
            {
                TenantId = tenantId,
                Name = "Indirect Income",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = true,
                Nature = AccountGroupNature.Income,
                GroupUnder = agPrimaryId
            };
            var agIndirectIncomeId = await accountGroupRepository.InsertAndGetIdAsync(indirectIncome);


            await accountLedgerRepository.InsertAsync(new AccountLedger
            {
                TenantId = tenantId,
                ParentId = null,
                Name = "Fine Applied",
                Narration = " ",
                Address = " ",
                Phone = " ",
                Email = " ",
                CreditPeriod = 0,
                Pan = " ",
                Status = true,
                IsDefault = true,
                CrOrDr = DrOrCr.Dr,
                AccountGroupId = agIndirectIncomeId
            });
            await accountLedgerRepository.InsertAsync(new AccountLedger
            {
                TenantId = tenantId,
                Name = "Discount Received",
                Narration = " ",
                Address = " ",
                Phone = " ",
                Email = " ",
                CreditPeriod = 0,
                Pan = " ",
                Status = true,
                IsDefault = true,
                CrOrDr = DrOrCr.Dr,
                AccountGroupId = agIndirectIncomeId
            });

            var indirectExpenses = new AccountGroup
            {
                TenantId = tenantId,
                Name = "Indirect Expenses",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = true,
                Nature = AccountGroupNature.Expenses,
                GroupUnder = agPrimaryId
            };
            var agIndirectExpenseId = await accountGroupRepository.InsertAndGetIdAsync(indirectExpenses);

            await accountLedgerRepository.InsertAsync(new AccountLedger
            {
                TenantId = tenantId,
                ParentId = null,
                Name = "Discount Allowed",
                Narration = " ",
                Address = " ",
                Phone = " ",
                Email = " ",
                CreditPeriod = 0,
                Pan = " ",
                Status = true,
                IsDefault = true,
                CrOrDr = DrOrCr.Dr,
                AccountGroupId = agIndirectExpenseId
            });


            var bankEmi = new AccountGroup
            {
                TenantId = tenantId,
                Name = "BankEMI",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Liabilities,
                GroupUnder = agPrimaryId
            };
            await accountGroupRepository.InsertAndGetIdAsync(bankEmi);

            await accountLedgerRepository.InsertAsync(new AccountLedger
            {
                TenantId = tenantId,
                Name = "Fine Paid",
                Narration = " ",
                Address = " ",
                Phone = " ",
                Email = " ",
                CreditPeriod = 0,
                Pan = " ",
                Status = true,
                IsDefault = true,
                CrOrDr = DrOrCr.Dr,
                AccountGroupId = agIndirectExpenseId
            });

            await accountLedgerRepository.InsertAsync(new AccountLedger
            {
                TenantId = tenantId,
                Name = "Interest Expenses",
                Narration = " ",
                Address = " ",
                Phone = " ",
                Email = " ",
                CreditPeriod = 0,
                Pan = " ",
                Status = true,
                IsDefault = true,
                CrOrDr = DrOrCr.Dr,
                AccountGroupId = agIndirectExpenseId
            });

            var salaryAccountGroup = new AccountGroup
            {
                TenantId = tenantId,
                Name = "Salary Account",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Expenses,
                GroupUnder = agIndirectExpenseId
            };
            await accountGroupRepository.InsertAndGetIdAsync(salaryAccountGroup);


            var transportExpenses = new AccountGroup
            {
                TenantId = tenantId,
                Name = "Transport Expenses",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Expenses,
                GroupUnder = agIndirectExpenseId
            };
            await accountGroupRepository.InsertAsync(transportExpenses);


            await accountLedgerRepository.InsertAsync(new AccountLedger
            {
                TenantId = tenantId,
                Name = "TransportExpenses",
                Narration = " ",
                Address = " ",
                Phone = " ",
                Email = " ",
                CreditPeriod = 0,
                Pan = " ",
                Status = true,
                IsDefault = true,
                CrOrDr = DrOrCr.Dr,
                AccountGroupId = agIndirectExpenseId
            });

            await accountLedgerRepository.InsertAsync(new AccountLedger
            {
                TenantId = tenantId,
                Name = "Salary",
                Narration = " ",
                Address = " ",
                Phone = " ",
                Email = " ",
                CreditPeriod = 0,
                Pan = " ",
                Status = true,
                IsDefault = true,
                CrOrDr = DrOrCr.Dr,

                AccountGroupId = agIndirectExpenseId
            });

            await accountGroupRepository.InsertAsync(new AccountGroup
            {
                TenantId = tenantId,
                Name = "Reservers & Surplus",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Liabilities,
                GroupUnder = agCapital
            });

            await accountGroupRepository.InsertAsync(new AccountGroup
            {
                TenantId = tenantId,
                Name = "Bank OD A/C",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Liabilities,
                GroupUnder = agLoanAndLiabitiesId
            });

            await accountGroupRepository.InsertAsync(new AccountGroup
            {
                TenantId = tenantId,
                Name = "Secured Loans",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Liabilities,
                GroupUnder = agLoanAndLiabitiesId
            });

            await accountGroupRepository.InsertAsync(new AccountGroup
            {
                TenantId = tenantId,
                Name = "Unsecured Loans",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Liabilities,
                GroupUnder = agLoanAndLiabitiesId
            });

            var dutiesAndTaxes = new AccountGroup
            {
                TenantId = tenantId,
                Name = "Duties & Taxes",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Liabilities,
                GroupUnder = agCurrentLiabilitiesId
            };
            var agDutiesAndTax = await accountGroupRepository.InsertAndGetIdAsync(dutiesAndTaxes);


            await accountGroupRepository.InsertAsync(new AccountGroup
            {
                TenantId = tenantId,
                Name = "Provisions",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Liabilities,
                GroupUnder = agCurrentLiabilitiesId
            });

            await accountGroupRepository.InsertAsync(new AccountGroup
            {
                TenantId = tenantId,
                Name = "Sundry Creditors",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Liabilities,
                GroupUnder = agCurrentLiabilitiesId
            });

            await accountGroupRepository.InsertAsync(new AccountGroup
            {
                TenantId = tenantId,
                Name = "Stock-in-Hand",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Assets,
                GroupUnder = agCurrentAssetsId // CurrentAssets.Id
            });

            await accountGroupRepository.InsertAsync(new AccountGroup
            {
                TenantId = tenantId,
                Name = "Deposits (Assets)",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Assets,
                GroupUnder = agCurrentAssetsId // CurrentAssets.Id
            });


            var loanadvance = new AccountGroup
            {
                TenantId = tenantId,
                Name = "Loans & Advances (Assets)",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Assets,
                GroupUnder = agCurrentAssetsId // CurrentAssets.Id
            };

            var agLoanorAdvanceId = await accountGroupRepository.InsertAndGetIdAsync(loanadvance);

            await accountLedgerRepository.InsertAsync(new AccountLedger
            {
                TenantId = tenantId,
                ParentId = null,
                Name = "Advance Payment",
                Narration = " ",
                Address = " ",
                Phone = " ",
                Email = " ",
                CreditPeriod = 0,
                Pan = " ",
                Status = true,
                IsDefault = true,
                CrOrDr = DrOrCr.Cr,
                AccountGroupId = agLoanorAdvanceId
            });

            await accountGroupRepository.InsertAsync(new AccountGroup
            {
                TenantId = tenantId,
                Name = "Sundry Debtors",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Assets,
                GroupUnder = agCurrentAssetsId
            });

            var cashinHand = new AccountGroup
            {
                TenantId = tenantId,
                Name = "Cash-in Hand",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Assets,
                GroupUnder = agCurrentAssetsId
            };
            var agCashinHandId = await accountGroupRepository.InsertAndGetIdAsync(cashinHand);


            await accountLedgerRepository.InsertAsync(new AccountLedger
            {
                TenantId = tenantId,
                ParentId = null,
                Name = "Cash",
                Narration = " ",
                Address = " ",
                Phone = " ",
                Email = " ",
                CreditPeriod = 0,
                Pan = " ",
                Status = true,
                IsDefault = true,
                CrOrDr = DrOrCr.Dr,

                AccountGroupId = agCashinHandId
            });
            await accountGroupRepository.InsertAsync(new AccountGroup
            {
                TenantId = tenantId,
                Name = "Bank Account",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Assets,
                GroupUnder = agCurrentAssetsId
            });

            var serviceAccount = new AccountGroup
            {
                TenantId = tenantId,
                Name = "Service Account",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = true,
                Nature = AccountGroupNature.Income,
                GroupUnder = agDirectIncome
            };
            var agServiceAccountId = await accountGroupRepository.InsertAndGetIdAsync(serviceAccount);

            await accountLedgerRepository.InsertAsync(new AccountLedger
            {
                TenantId = tenantId,
                ParentId = null,
                Name = "Service Account",
                Narration = " ",
                Address = " ",
                Phone = " ",
                Email = " ",
                CreditPeriod = 0,
                Pan = " ",
                Status = true,
                IsDefault = true,
                CrOrDr = DrOrCr.Cr,
                AccountGroupId = agServiceAccountId
            });

            await accountGroupRepository.InsertAsync(new AccountGroup
            {
                TenantId = tenantId,
                Name = "Employee",
                Narration = " ",
                IsDefault = true,
                AffectGrossProfit = false,
                Nature = AccountGroupNature.Liabilities,
                GroupUnder = agCurrentLiabilitiesId
            });

            // added by arjun
            var taxAccountLedger = new AccountLedger
            {
                Id = Guid.Empty,
                OpeningBalance = 0,
                ParentId = null,
                OpeningDate = null,
                CreditLimit = null,
                IsBillByBill = false,
                IsDelete = false,
                TenantId = tenantId,
                Name = "VAT Account",
                Narration = " ",
                Address = " ",
                Phone = " ",
                Email = " ",
                CreditPeriod = 0,
                Pan = " ",
                Status = true,
                IsDefault = true,
                CrOrDr = DrOrCr.Cr,
                AccountGroupId = agDutiesAndTax
            };
            var taxLedgerId = await accountLedgerRepository.InsertAndGetIdAsync(taxAccountLedger);
            await InitialTax(taxLedgerId, tenantId);

            var naTaxAccountLedger = new AccountLedger
            {
                Id = Guid.Empty,
                OpeningBalance = 0,
                ParentId = null,
                OpeningDate = null,
                CreditLimit = null,
                IsBillByBill = false,
                IsDelete = false,
                TenantId = tenantId,
                Name = "NA Tax Account",
                Narration = " ",
                Address = " ",
                Phone = " ",
                Email = " ",
                CreditPeriod = 0,
                Pan = " ",
                Status = true,
                IsDefault = true,
                CrOrDr = DrOrCr.Cr,

                AccountGroupId = agDutiesAndTax
            };
            var nataxLedgerId = await accountLedgerRepository.InsertAndGetIdAsync(naTaxAccountLedger);
            await NaInitialTax(nataxLedgerId, tenantId);
        }

        private async Task InitialTax(Guid taxLedgerId, int tenantId)
        {
            var tax1 = new Tax
            {
                Name = "Vat@13%",
                Rate = 13,
                Description = "",
                IsActive = true,
                LedgerId = taxLedgerId,
                TenantId = tenantId
            };
            await taxRepository.InsertAsync(tax1);
        }

        private async Task NaInitialTax(Guid taxLedgerId, int tenantId)
        {
            var tax = new Tax
            {
                Id = Guid.Empty,
                Name = "NA",
                Rate = 0,
                Description = "No Tax",
                IsActive = true,
                LedgerId = taxLedgerId,
                TenantId = tenantId
            };
            await taxRepository.InsertAsync(tax);
        }

        [UnitOfWork]
        private async Task InitialInventory(Guid branchId, int tenantId)
        {
            var productGroup = new ProductGroup
            {
                Name = "PRIMARY",
                GroupUnder = null,
                Description = "",
                IsDefult = true,
                TenantId = tenantId
            };
            await productGroupRepository.InsertAsync(productGroup);

            var unit2 = new Unit
            {
                Name = "Pcs",
                FormalName = "Pieces",
                TenantId = tenantId
            };
            await unitRepository.InsertAsync(unit2);

            var unit1 = new Unit
            {
                Name = "Kg",
                FormalName = "Kilogram",
                TenantId = tenantId
            };
            await unitRepository.InsertAsync(unit1);


            var stateList = new List<string>
            {
                "Province-1", "Madhesh Pradesh", "Bagmati Pradesh", "Gandaki Pradesh", "Lumbini Pradesh",
                "Karnali Pradesh", "Sudurpaschim Pradesh"
            };

        }

        private async Task AddVoucherTypeIfNotExistsAsync(VoucherType voucherType, Guid financialYearId, int tenantId)
        {
            if (await voucherTypeRepository.GetAll().IgnoreQueryFilters()
                    .AnyAsync(l => l.TenantId == voucherType.TenantId && l.Name == voucherType.Name)) return;
            var vtId = await voucherTypeRepository.InsertAndGetIdAsync(voucherType);

            if (voucherType.Name == "TI")
            {
                var voucherNumbering = new VoucherNumbering
                {
                    VoucherTypeId = vtId,
                    StartingIndex = 1,
                    Prefix = "TI",
                    Postfix = "",
                    VoucherGenerateType = VoucherGenerateType.Automatic,
                    FinancialYearId = financialYearId,
                    TenantId = tenantId
                };
                await voucherNumberingRepository.InsertAsync(voucherNumbering);
            }

            if (voucherType.Name == "ABT")
            {
                var voucherNumbering = new VoucherNumbering
                {
                    VoucherTypeId = vtId,
                    StartingIndex = 1,
                    Prefix = "ABT",
                    Postfix = "",
                    VoucherGenerateType = VoucherGenerateType.Automatic,
                    FinancialYearId = financialYearId,
                    TenantId = tenantId
                };
                await voucherNumberingRepository.InsertAsync(voucherNumbering);
            }
        }

        private static List<VoucherType> GetInitialvoucherType(int tenantId, Guid branchId)
        {
            return new List<VoucherType>
            {
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "OpeningBalance",
                    TypeOfVoucher = "OpeningBalance",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "OpeningStock",
                    TypeOfVoucher = "OpeningStock",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "ContraVoucher",
                    TypeOfVoucher = "ContraVoucher",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "PaymentVoucher",
                    TypeOfVoucher = "PaymentVoucher",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "ReceiptVoucher",
                    TypeOfVoucher = "ReceiptVoucher",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "JournalVoucher",
                    TypeOfVoucher = "JournalVoucher",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "PdcPayable",
                    TypeOfVoucher = "PdcPayable",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "PdcReceivable",
                    TypeOfVoucher = "PdcReceivable",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "PdcClearance",
                    TypeOfVoucher = "PdcClearance",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "PurchaseOrder",
                    TypeOfVoucher = "PurchaseOrder",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "MaterialReceipt",
                    TypeOfVoucher = "MaterialReceipt",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "RejectionOut",
                    TypeOfVoucher = "RejectionOut",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "PurchaseInvoice",
                    TypeOfVoucher = "PurchaseInvoice",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "PurchaseReturn",
                    TypeOfVoucher = "PurchaseReturn",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "SalesQuotation",
                    TypeOfVoucher = "SalesQuotation",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "SalesOrder",
                    TypeOfVoucher = "SalesOrder",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "DeliveryNote",
                    TypeOfVoucher = "DeliveryNote",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "Ticket",
                    TypeOfVoucher = "Ticket",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "SalesForTicket",
                    TypeOfVoucher = "SalesForTicket",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "PurchaseAdditionalCost",
                    TypeOfVoucher = "PurchaseAdditionalCost",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "RejectionIn",
                    TypeOfVoucher = "RejectionIn",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "SalesInvoice",
                    TypeOfVoucher = "SalesInvoice",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "SalesReturn",
                    TypeOfVoucher = "SalesReturn",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "ServiceVoucher",
                    TypeOfVoucher = "ServiceVoucher",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "CreditNote",
                    TypeOfVoucher = "CreditNote",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "DebitNote",
                    TypeOfVoucher = "DebitNote",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "StockJournal",
                    TypeOfVoucher = "StockJournal",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "PhysicalStock",
                    TypeOfVoucher = "PhysicalStock",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "DailySalaryVoucher",
                    TypeOfVoucher = "DailySalaryVoucher",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "MonthlySalaryVoucher",
                    TypeOfVoucher = "MonthlySalaryVoucher",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "AdvancePayment ",
                    TypeOfVoucher = "AdvancePayment ",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "StockIssue",
                    TypeOfVoucher = "StockIssue",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "StockReceipt",
                    TypeOfVoucher = "StockReceipt",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "ServiceQuotation",
                    TypeOfVoucher = "ServiceQuotation",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "ServiceOrder",
                    TypeOfVoucher = "ServiceOrder",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "ServiceReturn",
                    TypeOfVoucher = "ServiceReturn",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "ServiceDelivery",
                    TypeOfVoucher = "ServiceDelivery",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "CouponPurchase",
                    TypeOfVoucher = "CouponPurchase",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "CouponSale",
                    TypeOfVoucher = "CouponSale",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "FlexySale",
                    TypeOfVoucher = "FlexySale",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "StockRequest",
                    TypeOfVoucher = "StockRequest",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "StockTransfer",
                    TypeOfVoucher = "StockTransfer",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "LoanVoucher",
                    TypeOfVoucher = "LoanVoucher",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "LoanVoucherPay",
                    TypeOfVoucher = "LoanVoucherPay",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "AdvanceVoucher",
                    TypeOfVoucher = "AdvanceVoucher",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "AdvanceVoucherPay",
                    TypeOfVoucher = "AdvanceVoucherPay",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "ImeiReplacementVoucher",
                    TypeOfVoucher = "ImeiReplacementVoucher",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "ImeiCancelVoucher",
                    TypeOfVoucher = "ImeiCancelVoucher",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "ServiceReceipt",
                    TypeOfVoucher = "ServiceReceipt",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "WarrentyTransfer",
                    TypeOfVoucher = "WarrentyTransfer",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "STLSettlement",
                    TypeOfVoucher = "STLSettlement",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "BankSTL",
                    TypeOfVoucher = "BankSTL",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "BankSTLPaid",
                    TypeOfVoucher = "BankSTLPaid",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "BankEMI",
                    TypeOfVoucher = "BankEMI",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "LCLoan",
                    TypeOfVoucher = "LCLoan",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "ODLoan",
                    TypeOfVoucher = "ODLoan",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "EMIPay",
                    TypeOfVoucher = "EMIPay",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "WarrantyTransfer",
                    TypeOfVoucher = "WarrantyTransfer",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "WarrantyReceipt",
                    TypeOfVoucher = "WarrantyReceipt",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,

                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "ABT",
                    TypeOfVoucher = "ABT",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,
                },
                new()
                {
                    Id = Guid.Empty,
                    TenantId = tenantId,
                    Name = "TI",
                    TypeOfVoucher = "TI",
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true,
                }
            };
        }
    }
}
