export class DomHelper {
    static waitUntilElementIsReady(selector: string, callback: any, checkPeriod?: number): void {
        const selectors = selector.split(',');
        const elementCount = selectors.length;
        if (!checkPeriod) {
            checkPeriod = 100;
        }
        const checkExist = setInterval(() => {
            let foundElementCount = 0;
            for (let i = 0; i < selectors.length; i++) {
                const selector = selectors[i].trim();
                if (selector[0] === '#') {
                    const idSelector = selector.replace('#', '');
                    foundElementCount = foundElementCount + (document.getElementById(idSelector) ? 1 : 0);
                } else if (selector[0] === '.') {
                    const classSelector = selector.replace('.', '');
                    foundElementCount = foundElementCount + (document.getElementsByClassName(classSelector) ? 1 : 0);
                }
            }
            if (foundElementCount >= elementCount) {
                clearInterval(checkExist);
                callback();
            }
        }, checkPeriod);
    }
    static createElement(tag: string, attributes: any[]): any {
        const el = document.createElement(tag);
        for (let i = 0; i < attributes.length; i++) {
            const attribute = attributes[i];
            el.setAttribute(attribute.key, attribute.value);
        }
        return el;
    }
    static getElementByAttributeValue(tag: string, attribute: string, value: string) {
        const els = document.getElementsByTagName(tag);
        if (!els) {
            return undefined;
        }
        for (let i = 0; i < els.length; i++) {
            const el = els[i];
            if (el.getAttribute(attribute) === value) {
                return el;
            }
        }
        return undefined;
    }
}
