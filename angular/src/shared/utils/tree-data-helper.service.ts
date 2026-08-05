import { Injectable } from '@angular/core';

interface TreeNode {
    children?: TreeNode[];
    parent?: TreeNode;
    data?: {
        name?: string;
        id?: number | string;
        parentId?: number | string;
        code?: string;
        [key: string]: any;
    };
    [key: string]: any;
}

@Injectable()
export class TreeDataHelperService {
    private matchesSelector(node: TreeNode, selector: Partial<TreeNode>): boolean {
        return Object.keys(selector).every((key) => {
            if (key === 'data' && selector.data) {
                return Object.keys(selector.data).every((dataKey) => node.data?.[dataKey] === selector.data[dataKey]);
            }
            return (node as any)[key] === (selector as any)[key];
        });
    }

    findNode(data: TreeNode[], selector: Partial<TreeNode>): TreeNode | null {
        const nodes = data.filter((node) => this.matchesSelector(node, selector));
        if (nodes?.length === 1) {
            return nodes[0];
        }
        let foundNode: TreeNode | null = null;
        data.forEach((d) => {
            if (!foundNode && d.children) {
                foundNode = this.findNode(d.children, selector);
            }
        });
        return foundNode;
    }
    findParent(data: TreeNode[], nodeSelector: Partial<TreeNode>): TreeNode | null {
        const node = this.findNode(data, nodeSelector);
        if (!node) {
            return null;
        }
        return node.parent || null;
    }
    findChildren(data: TreeNode[], selector: Partial<TreeNode>): string[] {
        const traverseChildren = function (node: TreeNode): string[] {
            let names: string[] = [];
            if (node.children) {
                node.children.forEach((c) => {
                    if (c.data?.name) {
                        names.push(c.data.name);
                    }
                    names = names.concat(traverseChildren(c));
                });
            }
            return names;
        };
        const foundNode = this.findNode(data, selector);
        if (foundNode) {
            return traverseChildren(foundNode);
        } else {
            return [];
        }
    }
}
