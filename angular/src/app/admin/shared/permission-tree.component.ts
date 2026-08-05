import { Component, Input, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { PermissionTreeEditModel } from '@app/admin/shared/permission-tree-edit.model';
import { AppComponentBase } from '@shared/common/app-component-base';
import { FlatPermissionDto } from '@shared/service-proxies/service-proxies';
import { FormsModule } from '@angular/forms';
import { NgTemplateOutlet } from '@angular/common';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
export interface PermissionNode {
    name: string;
    displayName: string;
    parentName?: string;
    children: PermissionNode[];
    isGranted: boolean;
    isExpanded: boolean;
    isVisible: boolean;
    level: number;
}
@Component({
    selector: 'permission-tree',
    templateUrl: './permission-tree.component.html',
    styleUrls: ['./permission-tree.component.css'],
    imports: [FormsModule, NgTemplateOutlet, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class PermissionTreeComponent extends AppComponentBase implements OnInit {
    @Input() singleSelect: boolean;
    @Input() disableCascade: boolean;
    rootNodes: PermissionNode[] = [];
    filter = '';
    totalCount = 0;
    grantedCount = 0;

    ngOnInit(): void {}
    set editData(val: PermissionTreeEditModel) {
        if (!val) {
            return;
        }
        this.buildTree(val.permissions, val.grantedPermissionNames);
    }
    private buildTree(permissions: FlatPermissionDto[], grantedPermissionNames: string[]) {
        const nodeMap = new Map<string, PermissionNode>();
        this.rootNodes = [];
        this.totalCount = permissions.length;
        this.grantedCount = grantedPermissionNames.length;
        // Create nodes
        permissions.forEach((p) => {
            const node: PermissionNode = {
                name: p.name,
                displayName: p.displayName,
                parentName: p.parentName,
                children: [],
                isGranted: grantedPermissionNames.includes(p.name),
                isExpanded: true,
                isVisible: true,
                level: 0,
            };
            nodeMap.set(p.name, node);
        });
        // Build hierarchy
        nodeMap.forEach((node) => {
            if (node.parentName && nodeMap.has(node.parentName)) {
                const parent = nodeMap.get(node.parentName);
                parent.children.push(node);
                node.level = parent.level + 1;
            } else {
                this.rootNodes.push(node);
            }
        });
        this.sortNodes(this.rootNodes);
    }
    private sortNodes(nodes: PermissionNode[]) {
        nodes.sort((a, b) => a.displayName.localeCompare(b.displayName));
        nodes.forEach((n) => {
            if (n.children.length > 0) {
                this.sortNodes(n.children);
            }
        });
    }
    toggleNode(node: PermissionNode, event?: MouseEvent) {
        if (event) {
            event.stopPropagation();
        }
        node.isExpanded = !node.isExpanded;
    }
    onPermissionClick(node: PermissionNode) {
        node.isGranted = !node.isGranted;
        this.handleCascadingSelection(node);
        this.updateCounts();
    }
    private handleCascadingSelection(node: PermissionNode) {
        if (this.disableCascade) {
            return;
        }
        if (node.isGranted) {
            // Grant all children
            this.updateChildrenRecursive(node, true);
            // Grant all parents
            this.updateParentsRecursive(node, true);
        } else {
            // Revoke all children
            this.updateChildrenRecursive(node, false);
            // We don't necessarily revoke parents if one child is revoked in many permission systems,
            // but usually in ABP, if you revoke a parent, you revoke all children.
            // If you revoke a child, the parent can stay granted.
        }
    }
    private updateChildrenRecursive(node: PermissionNode, isGranted: boolean) {
        node.children.forEach((child) => {
            child.isGranted = isGranted;
            this.updateChildrenRecursive(child, isGranted);
        });
    }
    private updateParentsRecursive(node: PermissionNode, isGranted: boolean) {
        if (!node.parentName) {
            return;
        }
        const parent = this.findNodeByName(this.rootNodes, node.parentName);
        if (parent && !parent.isGranted && isGranted) {
            parent.isGranted = true;
            this.updateParentsRecursive(parent, true);
        }
    }
    private findNodeByName(nodes: PermissionNode[], name: string): PermissionNode | null {
        for (const node of nodes) {
            if (node.name === name) {
                return node;
            }
            const found = this.findNodeByName(node.children, name);
            if (found) {
                return found;
            }
        }
        return null;
    }
    filterPermissions(text: string): void {
        this.filterTextRecursive(this.rootNodes, text.toLowerCase());
    }
    private filterTextRecursive(nodes: PermissionNode[], filterText: string): boolean {
        let anyVisible = false;
        nodes.forEach((node) => {
            const matches =
                node.displayName.toLowerCase().includes(filterText) || node.name.toLowerCase().includes(filterText);
            const hasVisibleChild = this.filterTextRecursive(node.children, filterText);
            node.isVisible = matches || hasVisibleChild;
            if (node.isVisible) {
                anyVisible = true;
                if (filterText && hasVisibleChild) {
                    node.isExpanded = true;
                }
            }
        });
        return anyVisible;
    }
    getGrantedPermissionNames(): string[] {
        const names: string[] = [];
        this.collectGrantedNames(this.rootNodes, names);
        return names;
    }
    private collectGrantedNames(nodes: PermissionNode[], names: string[]) {
        nodes.forEach((node) => {
            if (node.isGranted) {
                names.push(node.name);
            }
            this.collectGrantedNames(node.children, names);
        });
    }
    selectAll() {
        this.updateChildrenRecursiveAll(this.rootNodes, true);
        this.updateCounts();
    }
    deselectAll() {
        this.updateChildrenRecursiveAll(this.rootNodes, false);
        this.updateCounts();
    }
    expandAll() {
        this.expandCollapseRecursive(this.rootNodes, true);
    }
    collapseAll() {
        this.expandCollapseRecursive(this.rootNodes, false);
    }
    private updateChildrenRecursiveAll(nodes: PermissionNode[], isGranted: boolean) {
        nodes.forEach((node) => {
            node.isGranted = isGranted;
            this.updateChildrenRecursiveAll(node.children, isGranted);
        });
    }
    private expandCollapseRecursive(nodes: PermissionNode[], isExpanded: boolean) {
        nodes.forEach((node) => {
            node.isExpanded = isExpanded;
            this.expandCollapseRecursive(node.children, isExpanded);
        });
    }
    private updateCounts() {
        this.grantedCount = this.getGrantedPermissionNames().length;
    }
}
