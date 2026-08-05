import { ApplicationRef } from '@angular/core';
import { createNewHosts } from '@angularclass/hmr';
export const hmrBootstrap = (module: any, bootstrap: () => Promise<ApplicationRef>) => {
    let appRef: ApplicationRef;
    module.hot.accept();
    bootstrap().then((ref) => (appRef = ref));
    module.hot.dispose(() => {
        const elements = appRef.components.map((c) => c.location.nativeElement);
        const makeVisible = createNewHosts(elements);
        appRef.destroy();
        makeVisible();
    });
};
