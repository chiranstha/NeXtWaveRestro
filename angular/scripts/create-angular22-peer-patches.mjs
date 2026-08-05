import { execFileSync } from 'node:child_process';
import { copyFileSync, mkdtempSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { basename, join } from 'node:path';

const vendorDir = join(process.cwd(), 'vendor', 'angular22-peer-patches');
const workDir = mkdtempSync(join(tmpdir(), 'angular22-peer-patches-'));
const npmCommand = process.platform === 'win32' ? 'npm.cmd' : 'npm';
const runNpm = (args, options) =>
    execFileSync(npmCommand, args, {
        ...options,
        shell: process.platform === 'win32',
    });

mkdirSync(vendorDir, { recursive: true });

const packages = [
    {
        name: 'abp-ng2-module',
        version: '13.0.0',
        localVersion: '13.0.0-angular22.0',
        peers: {
            '@angular/common': '^21.0.0 || ^22.0.0',
            '@angular/core': '^21.0.0 || ^22.0.0',
        },
    },
    {
        name: 'ngx-bootstrap',
        version: '21.2.1',
        localVersion: '21.2.1-angular22.0',
        peers: {
            '@angular/common': '^21.2.0 || ^22.0.0',
            '@angular/core': '^21.2.0 || ^22.0.0',
            '@angular/forms': '^21.2.0 || ^22.0.0',
        },
    },
    {
        name: 'ng2-file-upload',
        version: '10.0.0',
        localVersion: '10.0.0-angular22.0',
        peers: {
            '@angular/common': '^21.x.x || ^22.0.0',
            '@angular/core': '^21.x.x || ^22.0.0',
        },
    },
];

for (const pkg of packages) {
    const packOutput = runNpm(['pack', `${pkg.name}@${pkg.version}`, '--silent'], {
        cwd: workDir,
        encoding: 'utf8',
    }).trim();
    const tarballPath = join(workDir, packOutput.split(/\r?\n/).at(-1));
    const extractDir = join(workDir, `${pkg.name}-${pkg.localVersion}`);

    mkdirSync(extractDir, { recursive: true });
    execFileSync('tar', ['-xzf', tarballPath, '-C', extractDir], { stdio: 'inherit' });

    const packageDir = join(extractDir, 'package');
    const packageJsonPath = join(packageDir, 'package.json');
    const packageJson = JSON.parse(readFileSync(packageJsonPath, 'utf8'));

    packageJson.version = pkg.localVersion;
    packageJson.peerDependencies ??= {};
    Object.assign(packageJson.peerDependencies, pkg.peers);

    writeFileSync(packageJsonPath, `${JSON.stringify(packageJson, null, 2)}\n`);

    const patchedTarball = runNpm(['pack', '--silent'], {
        cwd: packageDir,
        encoding: 'utf8',
    })
        .trim()
        .split(/\r?\n/)
        .at(-1);
    const patchedTarballPath = join(packageDir, patchedTarball);

    copyFileSync(patchedTarballPath, join(vendorDir, patchedTarball));
    console.log(basename(patchedTarball));
}
