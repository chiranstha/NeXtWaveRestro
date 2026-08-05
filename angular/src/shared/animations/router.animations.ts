import { trigger, transition, style, animate } from '@angular/animations';
export const routerTransition = trigger('routerTransition', [
    transition('* <=> *', [
        style({ opacity: 0, transform: 'translateX(-100%)' }),
        animate('0.3s ease-in-out', style({ opacity: 1, transform: 'translateX(0)' })),
    ]),
]);
