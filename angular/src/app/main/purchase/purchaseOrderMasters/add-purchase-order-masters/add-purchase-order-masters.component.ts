import {
    Location,
} from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    ElementRef,
    EventEmitter,
    Injector,
    OnDestroy,
    OnInit,
    Output,
    QueryList,
    TemplateRef,
    ViewChild,
    ViewChildren,
} from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { NgSelectComponent } from '@ng-select/ng-select';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditPurchaseOrderMasterDto,
    DocumentDetailsDto,
    FileParameter,
    // GetUnitByProductListDto,
    // PurchaseOrderDetailsProductTableDto,
    PurchaseOrderMasterAccountLedgerTableDto,
    PurchaseOrderMasterProductListDto,
    PurchaseOrderMastersServiceProxy,
    TenantSettingsEditDto,
    TenantSettingsServiceProxy,
    UnitConversionServiceDto,
    UniversalDropdownDto,
} from '@shared/service-proxies/service-proxies';

import { ShortcutInput } from 'ng-keyboard-shortcuts';
import { lastValueFrom, Observable, Subject, timer } from 'rxjs';
import { finalize, first, takeUntil } from 'rxjs/operators';
import { NepaliDatepickerComponent } from '@app/shared/common/nepalidatepicker/nepali-datepicker-angular.component';
import { appModuleAnimation } from '@shared/animations/routerTransition';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'app-add-purchase-order-masters',
    templateUrl: './add-purchase-order-masters.component.html',
    styleUrls: ['./add-purchase-order-masters.component.css'],
    animations: [appModuleAnimation]
})
export class AddPurchaseOrderMastersComponent extends AppComponentBase implements OnInit, OnDestroy {
    modalRef?: BsModalRef;
    // file uploader
    expand: boolean;
    @ViewChild('fileInput') fileInput;
    imageFile: FileParameter;
    documents: DocumentDetailsDto[];
    displayedRow: any;
    imageId: number;
    inputValue: any;
    files: File;
    imageDocs: string | ArrayBuffer;
    pdfDocs: string | ArrayBuffer;
    imageType: string;
    title = 'Create Purchase Order';
    acceptedExtension: ['.jpg', '.jpeg', '.png', '.pdf'];
    imageView = true;
    pdfView = true;
    @ViewChild('cashParty') cashPartyEvent: NgSelectComponent;
    // enter
    @ViewChild('Date1') date1: NepaliDatepickerComponent;
    @ViewChildren('productName') productNameEvent: QueryList<NgSelectComponent>;
    @ViewChild('descriptions', { read: ElementRef }) descriptionEvent: ElementRef;
    @ViewChildren('qtys', { read: ElementRef }) qtyEvent: QueryList<ElementRef>;
    @ViewChildren('rates', { read: ElementRef }) rateEvent: QueryList<ElementRef>;
    @ViewChildren('amounts', { read: ElementRef }) amountEvent: QueryList<ElementRef>;
    @ViewChild('Selecter3') ngselect3: NgSelectComponent;
    @ViewChildren('unit') unitEvent: QueryList<NgSelectComponent>;
    /**
     * Key Event
     * @template {ViewChild} Propert decorator
     * @typedef { import {
    NgSelectComponent } from "@ng-select/ng-select" } NgSelectComponent
     * @typedef { import { NpxNpDatepickerComponent,
} from "@app/ne-datepicker/ne-datepicker.component"; } NpxNpDatepickerComponent
     */

    openMiti = false;
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    formLoader = true;
    accountLedgerId: string;
    form: FormGroup;
    ledgerId: string;
    //backspace

    textnull: boolean;
    numberflled: boolean;
    id: string;
    isAddMode: boolean;
    isManual = false;
    automaticInput = false;
    saving = false;
    units = false;
    codeDropdown$: Observable<PurchaseOrderMasterProductListDto[]>;
    purchaseOrderMaster: CreateOrEditPurchaseOrderMasterDto = new CreateOrEditPurchaseOrderMasterDto();
    // allProducts: any;
    allProducts$: Observable<UniversalDropdownDto[]>;
    productId: string;
    branchName = '';
    ledgerName = '';
    productCode = '';
    // allBranchs: PurchaseOrderMasterBranchTableDto[];
    allAccountLedgers: PurchaseOrderMasterAccountLedgerTableDto[];
    allAccountLedgers$: Observable<PurchaseOrderMasterAccountLedgerTableDto[]>;
    // allUnits: any[];
    allUnits$: Observable<UnitConversionServiceDto[]>;
    productUnit: any;


    serialNumber = 0;

    loadAPI: Promise<any>;
    allUnitNew: UniversalDropdownDto[];
    dateMiti: string;
    voucherNo: number;
    diffrenceDay = 0;
    nowDateTime: string;
    isPrint = false;
    settings: TenantSettingsEditDto = undefined;
    shortcuts: ShortcutInput[] = [];
    formDetailRowId: number | null = null;
    partyCreate: any = [
        {
            id: null,
            displayName: 'Add New',
        },
    ];
    purchaseI: any;
    private destroy$: Subject<void> = new Subject<void>();

    // FILEUPLOADER END
    constructor(
        private fb: FormBuilder,
        injector: Injector,
        private route: ActivatedRoute,
        private _location: Location,
        private router: Router,
        private modalService: BsModalService,
        private _tenantSettingsService: TenantSettingsServiceProxy,
        public modelRelatedApi: BsModalService,
        private _purchaseOrderMastersServiceProxy: PurchaseOrderMastersServiceProxy
    ) {
        super(injector);

        this.getSetting();




    }

    get purchaseOrderDetails() {
        return this.form.get('purchaseOrderDetails') as FormArray;

    }

    gotoDueDate(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.cashPartyEvent.close();
            document.getElementById('npDatePicker').focus();
        } else if (e.altKey || e.metaKey) {
            if (e.which === 67) {
                e.preventDefault();
                this.changeAl(e);
            }
        }
    }

    gotoDueDates(e) {
        if (e.which === 13) {
            e.preventDefault();
            document.getElementById('date').focus();
        }
    }

    gotoProduct(e) {
        if (e.which === 13) {
            e.preventDefault();
            setTimeout(() => {
                this.productNameEvent.first.open();
            }, 100);
        }
    }

    public formIsValid = () => false;

    onChangeBranch() {
        if (!this.id) {
            this.getVoucherNummber();
        }
        this.getAllProducts();
        this.getAllAcountLedgers();
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        // Complete the notifying Observable to remove it
        this.destroy$.complete();
    }

    getAllAcountLedgers(ledgerId?: number) {
        //  const branchId = this.form.get('branchId').value;
        this.allAccountLedgers$ = this._purchaseOrderMastersServiceProxy.getAllAccountLedgerForTableDropdown();
        this.allAccountLedgers$.subscribe((data) => {
            this.formLoader = false;
            if (this.serialNumber) {
                this.form.get('ledgerId').setValue(ledgerId);
                this.serialNumber = 0;
                setTimeout(() => {
                    this.cashPartyEvent.focus();
                }, 100);
            } else {
                if (!this.id) {
                    this.form.get('ledgerId').setValue(data[0].id);
                }
            }
        });
    }

    getAccountsLedger(e?: any) {
        this.serialNumber = 0;
        this._purchaseOrderMastersServiceProxy
            .getAllAccountLedgerForTableDropdown()
            .pipe(
                finalize(() => (this.formLoader = false)),
                takeUntil(this.destroy$)
            )
            .subscribe((data) => {
                if (e === undefined) {
                    this.allAccountLedgers = data;
                    if (!this.id) {
                        this.form.get('ledgerId').setValue(data[0].id);
                    }
                } else {
                    this.allAccountLedgers = data;
                    const n = data.length;
                    this.form.get('ledgerId').setValue(this.allAccountLedgers[0].id);
                }
            });
    }

    addReceiptDetails(e, i) {
        if (e.which === 13) {
            e.preventDefault();
            if (this.purchaseI === i) {
                if (this.purchaseOrderDetails.controls[i].valid) {
                    this.gotoNext(i);
                }
            } else {
                this.gotoNext(i);
            }
        } else if (e.key === 'Alt') {
            e.preventDefault();
            if (this.form.get('purchaseOrderDetails').valid) {
                for (const control of this.purchaseOrderDetails.controls) {
                    control.disable();
                }
                timer(100)
                    .pipe(first())
                    .subscribe(() => {
                        this.descriptionEvent.nativeElement.focus();
                    });
            }
        }
    }

    gotoNext(i) {
        this.keyEventsReceipt();
        //   this.checkFormArrdisableOnEdit(this.purchaseOrderDetails, i);
        timer(100)
            .pipe(first())
            .subscribe(() => {
                this.productNameEvent.last.open();
            });
    }

    getAllAcountLedger(): void {
        this.serialNumber = 0;
        this._purchaseOrderMastersServiceProxy
            .getAllAccountLedgerForTableDropdown()
            .pipe(
                finalize(() => (this.formLoader = false)),
                takeUntil(this.destroy$)
            )
            .subscribe((data) => {
                this.allAccountLedgers = data;

                if (!this.id) {
                    this.accountLedgerId = this.allAccountLedgers[0].id;
                    this.form.get('ledgerId').setValue(this.accountLedgerId);
                }
            });
    }

    displayFunc(obj: { productCode: string }): string {
        if (obj) {
            return obj.productCode.toLocaleUpperCase();
        }
    }

    ngOnInit(): void {
        this.id = this.route.snapshot.params['id'];

        this.createForm();
        this.changeVoucher();

        this.getSettingsForPrint();
        if (this.id && this.id !== undefined) {
            this.getAllUnitsAll();
            this.title = 'Edit Purchase Order';
            this.getData();
            this._purchaseOrderMastersServiceProxy.getPurchaseOrderMasterForEdit(this.id).subscribe((result) => {
                this.createForm(result);
                this.onChangeBranch();
                this.changeVoucher();
                this.diffDateMiti(result.dateMiti);
                this.diffdueDate(result.dueDateMiti);

                this.purchaseOrderDetails.controls.forEach((element) => {
                    element.disable();
                });
            });
        } else {
            this.form.get('dateMiti').setValue(this.today);
            this.form.get('dueDateMiti').setValue(this.today);
            this.onChangeBranch();
        }
    }

    getAllUnitsAll() {
        this.allUnits$ = this._purchaseOrderMastersServiceProxy.getAllUnits();
    }

    getVoucherNummber() {
        this._purchaseOrderMastersServiceProxy.getPurchaseOrderVoucherNo().subscribe((x) => {
            this.form.get('voucherNo').setValue(x);
        });
    }

    createForm(item: any = {}) {
        this.form = this.fb.group({
            voucherNo: [
                {
                    value: item.voucherNo ? item.voucherNo : this.voucherNo,
                    disabled: this.id,
                },
                Validators.required,
            ],
            dateMiti: [item.dateMiti ? item.dateMiti : this.today, Validators.required],
            dueDateMiti: [item.dueDateMiti ? item.dueDateMiti : this.today, Validators.required],
            cancelled: [item.cancelled ? item.cancelled : false],
            description: [item.description],
            ledgerId: [{ value: item.ledgerId ? item.ledgerId : this.ledgerId, disabled: this.id }, Validators.required],
            totalAmount: [item.totalAmount ? item.totalAmount : 0],
            purchaseOrderDetails: this.fb.array(
                (() => {
                    if (!item.purchaseOrderDetails) {
                        return [this.createpurchaseOrderDetails()];
                    }
                    return item.purchaseOrderDetails.map((item) => this.createpurchaseOrderDetails(item));
                })()
            ),
            id: [item.id],
        });
    }

    diffdueDate(e) {
        const endDateValue = e;
        const startDateValue = this.form.get('dateMiti').value;
        const Difference_In_Time = new Date(endDateValue).getTime() - new Date(startDateValue).getTime();
        this.diffrenceDay = Difference_In_Time / (1000 * 3600 * 24);
        // const itemStart = new NepaliDate(this.form.get("dateMiti").value).toJsDate();
        // const endDate = new NepaliDate(e).toJsDate();
        // this.diffrenceDay = moment(new Date(endDate)).diff(moment(new Date(itemStart)), "days");
    }

    diffDateMiti(e) {
        const startDateValue = e;
        const endDateValue = this.form.get('dueDateMiti').value;
        const Difference_In_Time = new Date(endDateValue).getTime() - new Date(startDateValue).getTime();
        this.diffrenceDay = Difference_In_Time / (1000 * 3600 * 24);
    }

    getpurchaseOrderDetails(form) {
        return form.controls.purchaseOrderDetails.controls;
    }

    removepurchaseOrderDetailsForm(i, form) {
        const control = form.controls.purchaseOrderDetails;
        if (control.length > 1) {
            control.removeAt(i);
        }
        this.totalAmountCalculation();
    }

    keyEventsReceipt(): void {
        this.enableAllpurchaseOrderDetails();
        if (this.form.get('purchaseOrderDetails').valid) {
            for (const control of this.purchaseOrderDetails.controls) {
                control.disable();
            }
            this.addpurchaseOrderDetailsForm();
        }
    }

    isFormValid(): boolean {
        return this.form.disabled ? true : this.form.valid;
    }

    addpurchaseOrderDetailsForm() {
        const control = <FormArray>this.form.controls.purchaseOrderDetails;
        control.push(this.createpurchaseOrderDetails());
        // this.ngselect2.last.open()
        this.totalAmountCalculation();
    }

    enablepurchaseOrderDetailsFormGroup(i, form) {
        this.purchaseI = i;
        this.purchaseOrderDetails.controls.forEach((data) => {
            if (data.valid) {
                data.disable();
            }
        });
        const control = form.controls.purchaseOrderDetails.controls[i];
        control.enable();
        const id = this.purchaseOrderDetails.controls[i].get('productId').value;
        this.getAllUnits(id);
        this.productNameEvent.toArray()[i].open();
    }

    enableAllpurchaseOrderDetails() {
        this.purchaseOrderDetails.controls.forEach((element) => {
            element.enable();
        });
    }

    totalAmountCalculation() {
        let netAmount = 0;
        for (let i = 0; i < this.purchaseOrderDetails.length; i++) {
            this.purchaseOrderDetails.controls[i]
                .get('amount')
                .setValue(this.purchaseOrderDetails.controls[i].get('qty').value * this.purchaseOrderDetails.controls[i].get('rate').value);
            netAmount = this.purchaseOrderDetails.controls[i].get('amount').value + netAmount;
        }
        this.form.get('totalAmount').setValue(netAmount);
    }

    rateCalculation() {
        let netAmount = 0;
        for (let i = 0; i < this.purchaseOrderDetails.length; i++) {
            this.purchaseOrderDetails.controls[i]
                .get('rate')
                .setValue(this.purchaseOrderDetails.controls[i].get('amount').value / this.purchaseOrderDetails.controls[i].get('qty').value);
            netAmount = this.purchaseOrderDetails.controls[i].get('amount').value + netAmount;
        }
        this.form.get('totalAmount').setValue(netAmount);
    }

    priceCalculation() {
        let netAmount = 0;
        for (let i = 0; i < this.purchaseOrderDetails.length; i++) {
            this.purchaseOrderDetails.controls[i]
                .get('amount')
                .setValue(this.purchaseOrderDetails.controls[i].get('rate').value * this.purchaseOrderDetails.controls[i].get('qty').value);
            netAmount = this.purchaseOrderDetails.controls[i].get('amount').value + netAmount;
        }
        this.form.get('totalAmount').setValue(netAmount);
    }

    save() {
        this.saving = false;
        if (this.serialNumber == 0) {
            if (this.form.valid) {
                if (this.id) {
                    this.message.confirm('', this.l('Do you want to Update ?'), (isConfirmed) => {
                        if (isConfirmed) {
                            this.apiCall();
                        }
                    });
                } else {
                    this.apiCall();
                }
            } else {
                this.notify.error('Form is invalid !!');
            }
        }
    }

    apiCall() {
        this.saving = true;
        this._purchaseOrderMastersServiceProxy
            .createOrEdit(this.form.getRawValue())
            .pipe(
                finalize(() => {
                    this.saving = false;
                })
            )
            .subscribe((data) => {
                if (this.isPrint === true) {
                    if (this.id) {
                        this.notify.info(this.l('Saved Successfully'));
                        this.router.navigate([`app/main/purchase/pdf/0/${this.id}`]);
                    } else {
                        this.notify.info(this.l('Saved Successfully'));
                        this.router.navigate([`app/main/purchase/pdf/0/${data}`]);
                    }
                } else {
                    if (this.id) {
                        this._location.back();
                        this.notify.info(this.l('Updated Successfully'));
                    } else {
                        this._location.back();
                        this.notify.info(this.l('SavedSuccessfully'));
                    }
                }
            });
    }

    close() {
        if (this.form.dirty) {
            this.message.confirm('', this.l('Do you want to Cancel ?'), (isConfirm) => {
                if (isConfirm) {
                    this._location.back();
                }
            });
        } else {
            this._location.back();
        }
    }

    getProductCode(id: string, abcd: FormGroup, i: number) {
        if (id == null) {
            this.formDetailRowId = i;
            this.serialNumber = 4;
        }
        this.getAllUnits(id);
        this._purchaseOrderMastersServiceProxy.getProductForView(id).subscribe((result) => {
            abcd.get('productName').setValue(result.name);
            // this.productUnit = result.units;
            abcd.get('qty').setValue(1);
            abcd.get('qty').enable();
            abcd.get('unitId').setValue(result.unitId);
            abcd.get('rate').setValue(result.purchaseRate);
            abcd.get('amount').setValue(abcd.get('qty').value * abcd.get('rate').value);
            this.totalAmountCalculation();
        });
    }

    onUnitConversion(event, abcd) {
        abcd.get('unitId').setValue(event.unitId);
        abcd.get('rate').setValue(event.rate);
        this.totalAmountCalculation();
    }

    getAllUnits(id) {
        this.allUnits$ = this._purchaseOrderMastersServiceProxy.getAllUnitsByProductId(id);
    }

    keyEventRate(event, i) {
        if (event.which === 13) {
            event.preventDefault();
            this.amountEvent.toArray()[i].nativeElement.focus();
        }
    }

    keyEventProductUnit(event: any, i) {
        if (event.which === 13) {
            event.preventDefault();
            this.rateEvent.toArray()[i].nativeElement.focus();
        }
    }

    changeAl(event) {
        this.serialNumber = event ? 5 : 0;
    }

    //   2 ledger
    keyEventLedger(event) {
        if (event.altKey || event.metaKey) {
            if (event.which === 67) {
                this.router.navigate(['app/main/accounting/accountLedgers/add']);
            }
        }
    }

    // shortcut keys

    //   3 product
    keyEventProduct(event, i) {
        if (event.altKey || event.metaKey) {
            if (event.which === 67) {
                event.preventDefault();
                this.formDetailRowId = i;
                this.changeProduct(event);
            }
        } else if (event.which === 13) {
            event.preventDefault();
            this.productNameEvent.toArray()[i].close();
            this.qtyEvent.toArray()[i].nativeElement.focus();
        }
    }

    goToUnit(e, i) {

    }

    changeProduct(event: any) {
        this.serialNumber = event ? 4 : 0;
    }

    getAllProducts(productId?: string | null) {
        this.allProducts$ = this._purchaseOrderMastersServiceProxy.getAllProductForTableDropdown();
        if (!this.serialNumber) {
            return;
        }

        const rowIndex = this.formDetailRowId;
        this.serialNumber = 0;
        this.formDetailRowId = null;

        if (!productId || rowIndex === null || !this.purchaseOrderDetails.controls[rowIndex]) {
            return;
        }

        const purchaseOrderDetail = this.purchaseOrderDetails.controls[rowIndex] as FormGroup;
        purchaseOrderDetail.get('productId')?.setValue(productId);
        setTimeout(() => {
            this.getProductCode(productId, purchaseOrderDetail, rowIndex);
            this.productNameEvent.first?.focus();
        }, 200);
    }

    getSettingsForPrint(): void {
        this._tenantSettingsService.getAllSettings().subscribe((result: TenantSettingsEditDto) => {
            this.settings = result;
            // if (this.settings.allSettingsBundleDto.tickPrintAfterSave == true) {
            //     this.isPrint = true;
            // }
        });
    }

    //4 units
    keyEventUnits(event) {
        if (event.altKey || event.metaKey) {
            if (event.which === 67) {
                this.router.navigate(['app/main/inventory/units/add']);
            }
        }
    }

    // 5 Racks
    keyEventRacks(event) {
        if (event.altKey || event.metaKey) {
            if (event.which === 67) {
                this.router.navigate(['app/main/inventory/racks/add']);
            }
        }
    }

    // keyEventSave(event) {
    //     if (event.altKey || event.metaKey) {
    //         if (event.which === 83) {
    //             event.preventDefault();
    //             if (this.form.valid) {
    //                 this.save();
    //             }
    //         }
    //     }
    // }

    keyClose(event: boolean) {
        if (this.serialNumber) {
            this.serialNumber = 0;
        } else {
            if (event === true) {
                this.close();
            } else {
                return;
            }
        }
    }

    keySave(event: boolean) {
        if (event === true) {
            this.save();
        } else {
            return;
        }
    }

    // edit delete button
    clearForm(id) {
        this.message.confirm('', this.l('Are you sure you want to Delete ?'), (isConfirmed) => {
            if (isConfirmed) {
                this._purchaseOrderMastersServiceProxy.delete(id).subscribe(() => {
                    this._location.back();
                    this.notify.success(this.l('Deleted Successfully'));
                });
            }
        });
    }

    fromVoucherNo() {
        this.cashPartyEvent.open();
    }

    gotoQty(i) {
        const qty = document.getElementById(`qty${  i}`);
        qty.focus();
    }

    gotoRate() {
        const item = <HTMLInputElement>document.getElementById('rate');
        item.focus();
    }

    gotoUnit() {
        this.ngselect3.open();
    }

    onSaveOnInput(event: any) {
        if (event.which === 13) {
            event.preventDefault();
            if (this.form.valid) {
                if (this.id) {
                    this.save();
                } else {
                    if (this.form.valid) {
                        this.message.confirm('', this.l('Do you want to Save ?'), (isConfirmed) => {
                            if (isConfirmed) {
                                this.save();
                            }
                        });
                    } else {
                        this.save();
                    }
                }
            } else {
                this.notify.error('Form is invalid !!');
            }
        }
    }

    changeVoucher() {
        //  if (!this.id) {
        const vouchergenerate$ = this._purchaseOrderMastersServiceProxy.getVoucherGenerateType();
        vouchergenerate$.pipe(first()).subscribe((x) => {
            if (x == 'Automatic') {
                this.automaticInput = true;
                this.getVoucherNummber();
            } else {
                this.automaticInput = false;
                if (x == 'Manual') {
                    this.isManual = true;
                } else {
                    this.isManual = false;
                }
            }
        });
        //   }
    }

    async checkSave() {
        this.saving = true;
        if (this.isManual) {
            const branchI = this.form.get('branchId').value;
            const voucherN = this.form.get('voucherNo').value;
            await lastValueFrom(this._purchaseOrderMastersServiceProxy.getCheckVoucherNo(voucherN)).then((value) => {
                if (!value) {
                    this.save();
                } else {
                    alert('Voucher Number already Exists');
                }
            });
        } else {
            this.save();
        }
    }

    // FOR FILEUPLOADER START
    onSingleImage(event) {
        const formData = new FormData();
        const purchase = this.id;
        formData.append('file', event.target.files[0]);
        if (event.target.files?.length) {
            const [file] = event.target.files;
            this.imageFile = { data: event.target.files[0], fileName: file.name };
            const extensionType = file.type;
            if (extensionType == 'image/jpeg' || extensionType == 'image/png' || extensionType == 'image/jpg' || extensionType == 'application/pdf') {
                this._purchaseOrderMastersServiceProxy.uploadImageNew(purchase, this.imageFile).subscribe((result) => {
                    this.getData();
                });
            } else {
                this.notify.info('Only jpeg, jpg, png, pdf files are allowed');
            }
        }
    }

    getData() {
        this._purchaseOrderMastersServiceProxy.getAllDocuments(this.id).subscribe((result) => {
            this.documents = result;
            this.displayedRow = this.documents;
            this.imageId = this.displayedRow.id;
        });
    }

    getImages(template: TemplateRef<any>, id) {
        // const dialogRef = this._dialogService.open(template, { responsivePadding: true });
        // this._purchaseOrderMastersServiceProxy.getImage(id).subscribe((result) => {
        //     this.imageType = result.fileType;
        //     if (this.imageType == '.pdf') {
        //         this.pdfView = true;
        //         this.imageView = false;
        //         const base64_string = result.image;
        //         this.pdfDocs = 'data:application/pdf;base64,' + base64_string;
        //     } else {
        //         this.pdfView = false;
        //         this.imageView = true;
        //         const base64_string = result.image;
        //         this.imageDocs = 'data:image/png;base64,' + base64_string;
        //     }
        // });
    }

    deleteFile(id) {
        this.message.confirm('', this.l('Are you sure you want to Delete ?'), (isConfirmed) => {
            if (isConfirmed) {
                this._purchaseOrderMastersServiceProxy.deleteFile(id).subscribe((result) => {
                    this.getData();
                });
            }
        });
    }

    openExpand(event) {
        if (event === true) {
            this.expand = true;
        } else {
            this.expand = false;
        }
    }

    private createpurchaseOrderDetails(item: any = {}) {
        return this.fb.group({
            qty: [item.qty ? item.qty : 0, Validators.required],
            rate: [item.rate ? item.rate : 0, Validators.required],
            amount: [item.amount ? item.amount : 0, Validators.required],
            productId: [item.productId ? item.productId : this.emptyGuId, Validators.required],
            productName: [item.productName ? item.productName : '', Validators.required],
            unitId: [item.unitId ? item.unitId : this.emptyGuId, Validators.required],
            id: [item.id ? item.id : this.emptyGuId],
        });
    }

    // FOR FILEUPLOADER END
}
