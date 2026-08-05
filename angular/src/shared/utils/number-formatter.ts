export function formatNumberIndian(value: number | string, decimals = 2): string {
    const n = Number(value) || 0;
    const sign = n < 0 ? '-' : '';
    const abs = Math.abs(n);
    // Ensure decimals
    const parts = abs.toFixed(decimals).split('.');
    let intPart = parts[0];
    const decPart = parts[1] ? `.${parts[1]}` : '';
    // apply Indian (lakh/crore) grouping: last 3, then groups of 2
    const lastThree = intPart.slice(-3);
    let other = intPart.slice(0, -3);
    if (other !== '') {
        other = other.replace(/\B(?=(\d{2})+(?!\d))/g, ',');
        intPart = `${other},${lastThree}`;
    } else {
        intPart = lastThree;
    }
    return sign + intPart + decPart;
}
export function formatNepali(
    value: number | string,
    decimals = 2,
    useNepaliDigits = true,
    useParenForNegative = true,
): string {
    const n = Number(value) || 0;
    const isNegative = n < 0;
    const formatted = formatNumberIndian(Math.abs(n), decimals);
    const withDigits = formatted;
    if (isNegative) {
        if (useParenForNegative) {
            return `(${withDigits})`;
        }
        // add minus sign (Nepali minus not needed): use '-' char
        return `-${withDigits}`;
    }
    return withDigits;
}
