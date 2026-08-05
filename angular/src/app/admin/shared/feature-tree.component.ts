import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { FeatureTreeEditModel } from '@app/admin/shared/feature-tree-edit.model';
import { AppComponentBase } from '@shared/common/app-component-base';
import { FlatFeatureDto, NameValueDto } from '@shared/service-proxies/service-proxies';
import { ArrayToTreeConverterService } from '@shared/utils/array-to-tree-converter.service';
import { TreeDataHelperService } from '@shared/utils/tree-data-helper.service';
import { TreeNode, AppTemplate } from '@shared/ui-compat';
import { Tree } from '@shared/ui-compat';
import { FormsModule } from '@angular/forms';
@Component({
    selector: 'feature-tree',
    templateUrl: './feature-tree.component.html',
    styleUrls: ['./feature-tree.component.less'],
    imports: [Tree, AppTemplate, FormsModule],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class FeatureTreeComponent extends AppComponentBase {
    private _arrayToTreeConverterService = inject(ArrayToTreeConverterService);
    private _treeDataHelperService = inject(TreeDataHelperService);
    treeData: any;
    selectedFeatures: TreeNode[] = [];

    _editData: FeatureTreeEditModel;
    set editData(val: FeatureTreeEditModel) {
        this._editData = val;
        this.setTreeData(val.features);
        this.setSelectedNodes(val);
    }
    setTreeData(permissions: FlatFeatureDto[]) {
        this.treeData = this._arrayToTreeConverterService.createTree(
            permissions,
            'parentName',
            'name',
            null,
            'children',
            [
                {
                    target: 'label',
                    source: 'displayName',
                },
                {
                    target: 'expandedIcon',
                    value: 'fa fa-folder-open text-warning',
                },
                {
                    target: 'collapsedIcon',
                    value: 'fa fa-folder text-warning',
                },
                {
                    target: 'expanded',
                    value: true,
                },
                {
                    target: 'selectable',
                    targetFunction(item) {
                        return item.inputType.name === 'CHECKBOX';
                    },
                },
            ],
        );
    }
    setSelectedNodes(val: FeatureTreeEditModel) {
        this.selectedFeatures = [];
        val.features.forEach((feature) => {
            const items = val.featureValues.filter((fv) => fv.name === feature.name);
            if (items?.length === 1) {
                const item = items[0];
                this.setSelectedNode(item.name, item.value);
            } else {
                this.setSelectedNode(feature.name, feature.defaultValue);
            }
        });
    }
    setSelectedNode(featureName, value) {
        let node;
        if (value === 'true') {
            node = this._treeDataHelperService.findNode(this.treeData, { data: { name: featureName } });
            this.selectedFeatures.push(node);
        } else if (value && value !== 'false') {
            node = this._treeDataHelperService.findNode(this.treeData, { data: { name: featureName } });
            node.value = value;
            this.selectedFeatures.push(node);
        }
    }
    getGrantedFeatures(): NameValueDto[] {
        if (!this._editData.features) {
            return [];
        }
        const features: NameValueDto[] = [];
        for (let i = 0; i < this._editData.features.length; i++) {
            const feature = new NameValueDto();
            feature.name = this._editData.features[i].name;
            feature.value = this.getFeatureValueByName(feature.name);
            features.push(feature);
        }
        return features;
    }
    onDropdownChange(node) {
        if (node.value) {
            node.selected = true;
        }
    }
    onInputChange(node) {
        if (node.value) {
            node.selected = true;
        }
    }
    findFeatureByName(featureName: string): FlatFeatureDto {
        const self = this;
        const feature = self._editData.features.find((f) => f.name === featureName);
        if (!feature) {
            abp.log.warn(`Could not find a feature by name: ${featureName}`);
        }
        return feature;
    }
    findFeatureValueByName(featureName: string) {
        const self = this;
        const feature = self.findFeatureByName(featureName);
        if (!feature) {
            return '';
        }
        const featureValue = self._editData.featureValues.find((f) => f.name === featureName);
        if (!featureValue) {
            return feature.defaultValue;
        }
        return featureValue.value;
    }
    isFeatureValueValid(featureName: string, value: string): boolean {
        const self = this;
        const feature = self.findFeatureByName(featureName);
        if (!feature?.inputType?.validator) {
            return true;
        }
        const validator = feature.inputType.validator as any;
        if (validator.name === 'STRING') {
            if (value === undefined || value === null) {
                return validator.attributes.AllowNull;
            }
            if (typeof value !== 'string') {
                return false;
            }
            if (validator.attributes.MinLength > 0 && value.length < validator.attributes.MinLength) {
                return false;
            }
            if (validator.attributes.MaxLength > 0 && value.length > validator.attributes.MaxLength) {
                return false;
            }
            if (validator.attributes.RegularExpression) {
                return new RegExp(validator.attributes.RegularExpression).test(value);
            }
        } else if (validator.name === 'NUMERIC') {
            const numValue = parseInt(value);
            if (isNaN(numValue)) {
                return false;
            }
            const minValue = validator.attributes.MinValue;
            if (minValue > numValue) {
                return false;
            }
            const maxValue = validator.attributes.MaxValue;
            if (maxValue > 0 && numValue > maxValue) {
                return false;
            }
        }
        return true;
    }
    areAllValuesValid(): boolean {
        let result = true;
        this._editData.features.forEach((feature) => {
            const value = this.getFeatureValueByName(feature.name);
            if (!this.isFeatureValueValid(feature.name, value)) {
                result = false;
            }
        });
        return result;
    }
    setFeatureValueByName(featureName: string, value: string): void {
        const featureValue = this._editData.featureValues.find((f) => f.name === featureName);
        if (!featureValue) {
            return;
        }
        featureValue.value = value;
    }
    isFeatureSelected(name: string): boolean {
        const nodes = this.selectedFeatures.filter((f) => f.data.name === name);
        return nodes?.length === 1;
    }
    getFeatureValueByName(featureName: string): string {
        const feature = this._treeDataHelperService.findNode(this.treeData, { data: { name: featureName } });
        if (!feature) {
            return null;
        }
        if (feature.value) {
            return feature.value;
        }
        if (!this.isFeatureSelected(featureName)) {
            return 'false';
        }
        return 'true';
    }
    isFeatureEnabled(featureName: string): boolean {
        const self = this;
        const value = self.findFeatureValueByName(featureName);
        return value.toLowerCase() === 'true';
    }
    nodeSelect(event) {
        let parentNode = this._treeDataHelperService.findParent(this.treeData, {
            data: { name: event.node.data.name },
        });
        while (parentNode != null) {
            const isParentNodeAdded = this.selectedFeatures.find((f) => f.data.name === parentNode.data.name);
            if (!isParentNodeAdded) {
                this.selectedFeatures.push(parentNode);
            }
            parentNode = this._treeDataHelperService.findParent(this.treeData, {
                data: { name: parentNode.data.name },
            });
        }
    }
    onNodeUnselect(event) {
        const childrenNodes = this._treeDataHelperService.findChildren(this.treeData, {
            data: { name: event.node.data.name },
        });
        childrenNodes.push(event.node.data.name);
        this.selectedFeatures = this.selectedFeatures.filter((x) => childrenNodes.indexOf(x.data.name) === -1);
    }
}
