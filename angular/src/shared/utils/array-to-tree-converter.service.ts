import { Injectable } from '@angular/core';
@Injectable()
export class ArrayToTreeConverterService {
    createTree(
        array: any[],
        parentIdProperty,
        idProperty,
        parentIdValue,
        childrenProperty: string,
        fieldMappings,
    ): any {
        const tree = [];
        const nodes = array.filter((item) => item[parentIdProperty] === parentIdValue);
        nodes.forEach((node) => {
            const newNode = {
                data: node,
            };
            this.mapFields(node, newNode, fieldMappings);
            newNode[childrenProperty] = this.createTree(
                array,
                parentIdProperty,
                idProperty,
                node[idProperty],
                childrenProperty,
                fieldMappings,
            );
            tree.push(newNode);
        });
        return tree;
    }
    mapFields(node, newNode, fieldMappings): void {
        fieldMappings.forEach((fieldMapping) => {
            if (!fieldMapping['target']) {
                return;
            }
            if (fieldMapping.hasOwnProperty('value')) {
                newNode[fieldMapping['target']] = fieldMapping['value'];
            } else if (fieldMapping['source']) {
                newNode[fieldMapping['target']] = node[fieldMapping['source']];
            } else if (fieldMapping['targetFunction']) {
                newNode[fieldMapping['target']] = fieldMapping['targetFunction'](node);
            }
        });
    }
}
