export function actionCellRenderer(params: any): string {
    const isGrouped = params.api.getRowGroupColumns().length > 0;
    if (isGrouped && params.colDef?.cellRendererParams?.hideDeleteOnGroup) {
        params.api.setColumnsVisible(['actions'], false);
        return '';
    }

    const iconSize = '0.9em';
    const viewButton = `<button type="button" class="btnaction btn-view" data-row-index="${params.data.id}" title="View" style="background: none; border: none; cursor: pointer;"><i class="fas fa-eye" style="color:#007bff; font-size: ${iconSize};"></i></button>`;
    const editButton = `<button type="button" class="btnaction btn-edit" data-row-index="${params.data.id}" title="Edit" style="background: none; border: none; cursor: pointer;"><i class="fas fa-pen" style="color:#ffc107; font-size: ${iconSize};"></i></button>`;
    const deleteButton = `<button type="button" class="btnaction btn-delete cursor-pointer" data-row-index="${params.data.id}" title="Delete" style="background: none; border: none; cursor: pointer;"><i class="fas fa-trash" style="color:#dc3545; font-size: ${iconSize};"></i></button>`;
    return `${viewButton} ${editButton} ${deleteButton}`;
}
