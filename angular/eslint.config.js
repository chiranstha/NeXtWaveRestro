import js from '@eslint/js';
import tseslint from '@typescript-eslint/eslint-plugin';
import tsParser from '@typescript-eslint/parser';
import angularEslint from '@angular-eslint/eslint-plugin';
import angularTemplateEslint from '@angular-eslint/eslint-plugin-template';
import angularTemplateParser from '@angular-eslint/template-parser';
import unusedImports from 'eslint-plugin-unused-imports';

export default [
    {
        ignores: [
            'src/assets/**',
            'src/environments/**',
            'src/nswag/**',
            'src/**/*.spec.ts',
            'src/**/*.test.ts',
            'src/assets/metronic/**',
        ],
    },
    js.configs.recommended,
    {
        files: ['src/**/*.ts'],
        languageOptions: {
            parser: tsParser,
            parserOptions: {
                projectService: true,
                sourceType: 'module',
            },
            globals: {
                window: 'readonly',
                document: 'readonly',
                console: 'readonly',
                process: 'readonly',
                Buffer: 'readonly',
                __dirname: 'readonly',
                __filename: 'readonly',
                global: 'readonly',
                module: 'readonly',
                require: 'readonly',
                exports: 'readonly',
                // Browser globals
                setTimeout: 'readonly',
                clearTimeout: 'readonly',
                setInterval: 'readonly',
                clearInterval: 'readonly',
                location: 'readonly',
                navigator: 'readonly',
                localStorage: 'readonly',
                sessionStorage: 'readonly',
                alert: 'readonly',
                confirm: 'readonly',
                prompt: 'readonly',
                atob: 'readonly',
                btoa: 'readonly',
                // ABP framework global
                abp: 'readonly',
                // Other globals
                KTUtil: 'readonly',
                KTApp: 'readonly',
                fetch: 'readonly',
                history: 'readonly',
                FormValidation: 'readonly',
                Swal: 'readonly',
                $: 'readonly',
            },
        },
        plugins: {
            '@typescript-eslint': tseslint,
            '@angular-eslint': angularEslint,
            '@angular-eslint/template': angularTemplateEslint,
            'unused-imports': unusedImports,
        },
        rules: {
            ...tseslint.configs.recommended.rules,
            // Disable prefer-standalone as project uses modules
            '@angular-eslint/prefer-standalone': 'off',
            // Allow any for now but warn
            '@typescript-eslint/no-explicit-any': 'warn',
            // Allow unused vars but warn
            '@typescript-eslint/no-unused-vars': 'off',
            // Unused imports
            'unused-imports/no-unused-imports': 'error',
            'unused-imports/no-unused-vars': ['error', { varsIgnorePattern: '^_', argsIgnorePattern: '^_' }],
            // Allow triple slash references
            '@typescript-eslint/triple-slash-reference': 'off',
            // Allow empty lifecycle methods
            '@angular-eslint/no-empty-lifecycle-method': 'off',
            // Allow this aliasing
            '@typescript-eslint/no-this-alias': 'off',
            // Allow prototype builtins
            'no-prototype-builtins': 'off',
            // Allow component class suffix issues
            '@angular-eslint/component-class-suffix': 'off',
            // Allow lifecycle interface not implemented
            '@angular-eslint/use-lifecycle-interface': 'off',
            // Allow input rename
            '@angular-eslint/no-input-rename': 'off',
            // Allow output on prefix
            '@angular-eslint/no-output-on-prefix': 'off',
            // Allow no empty object type
            '@typescript-eslint/no-empty-object-type': 'off',

            // Enhanced code quality rules
            '@typescript-eslint/prefer-nullish-coalescing': 'warn',
            '@typescript-eslint/prefer-optional-chain': 'warn',
            '@typescript-eslint/no-unnecessary-type-assertion': 'warn',
            '@typescript-eslint/prefer-as-const': 'warn',
            '@typescript-eslint/no-floating-promises': 'warn',
            '@typescript-eslint/await-thenable': 'warn',

            // Performance rules
            '@angular-eslint/prefer-on-push-component-change-detection': 'warn',
            '@angular-eslint/use-component-selector': 'warn',

            // Security rules
            '@typescript-eslint/no-implied-eval': 'error',
            'no-eval': 'error',

            // Code style rules
            'no-console': [
                'error',
                {
                    allow: [
                        'log',
                        'warn',
                        'dir',
                        'timeLog',
                        'assert',
                        'clear',
                        'count',
                        'countReset',
                        'group',
                        'groupEnd',
                        'table',
                        'dirxml',
                        'error',
                        'groupCollapsed',
                        'Console',
                        'profile',
                        'profileEnd',
                        'timeStamp',
                        'context',
                    ],
                },
            ],
            'no-debugger': 'error',
            'no-unused-labels': 'error',
            'no-var': 'error',
            quotes: ['error', 'single'],
            curly: 'error',
            eqeqeq: ['warn', 'smart'],
            'guard-for-in': 'error',
            'no-bitwise': 'error',
            'no-caller': 'error',
            'no-eval': 'off',
            'no-fallthrough': 'error',
            'no-new-wrappers': 'error',
            'no-restricted-imports': 'error',
            'no-throw-literal': 'off',
            'no-trailing-spaces': 'error',
            'no-undef-init': 'error',
            'no-underscore-dangle': 'off',
            'no-unused-expressions': 'warn',
            'valid-typeof': 'error',
            'no-useless-escape': 'warn',
            'no-case-declarations': 'warn',
            'no-extra-boolean-cast': 'warn',
            '@typescript-eslint/no-wrapper-object-types': 'warn',

            // Additional modern rules
            'prefer-const': 'error',
            'no-const-assign': 'error',
            'prefer-arrow-callback': 'warn',
            'prefer-template': 'warn',
            'object-shorthand': 'warn',
            'prefer-destructuring': ['warn', { 'object': true, 'array': false }],
        },
    },
    {
        files: ['src/**/*.spec.ts'],
        languageOptions: {
            parser: tsParser,
            parserOptions: {
                projectService: true,
                sourceType: 'module',
            },
            globals: {
                window: 'readonly',
                document: 'readonly',
                console: 'readonly',
                process: 'readonly',
                Buffer: 'readonly',
                __dirname: 'readonly',
                __filename: 'readonly',
                global: 'readonly',
                module: 'readonly',
                require: 'readonly',
                exports: 'readonly',
                // Browser globals
                setTimeout: 'readonly',
                clearTimeout: 'readonly',
                setInterval: 'readonly',
                clearInterval: 'readonly',
                location: 'readonly',
                navigator: 'readonly',
                localStorage: 'readonly',
                sessionStorage: 'readonly',
                alert: 'readonly',
                confirm: 'readonly',
                prompt: 'readonly',
                atob: 'readonly',
                btoa: 'readonly',
                // ABP framework global
                abp: 'readonly',
                // Other globals
                KTUtil: 'readonly',
                KTApp: 'readonly',
                fetch: 'readonly',
                history: 'readonly',
                FormValidation: 'readonly',
                Swal: 'readonly',
                $: 'readonly',
                // Jasmine globals
                describe: 'readonly',
                beforeEach: 'readonly',
                afterEach: 'readonly',
                it: 'readonly',
                expect: 'readonly',
                jasmine: 'readonly',
                spyOn: 'readonly',
                fail: 'readonly',
                pending: 'readonly',
                xdescribe: 'readonly',
                xit: 'readonly',
                fit: 'readonly',
            },
        },
        plugins: {
            '@typescript-eslint': tseslint,
            '@angular-eslint': angularEslint,
        },
        rules: {
            ...tseslint.configs.recommended.rules,
            // Disable prefer-standalone as project uses modules
            '@angular-eslint/prefer-standalone': 'off',
            // Allow any for now
            '@typescript-eslint/no-explicit-any': 'warn',
            // Allow unused vars
            '@typescript-eslint/no-unused-vars': 'warn',
            // Allow triple slash references
            '@typescript-eslint/triple-slash-reference': 'off',
            // Allow empty lifecycle methods
            '@angular-eslint/no-empty-lifecycle-method': 'off',
            // Allow this aliasing
            '@typescript-eslint/no-this-alias': 'off',
            // Allow prototype builtins
            'no-prototype-builtins': 'off',
            // Allow component class suffix issues
            '@angular-eslint/component-class-suffix': 'off',
            // Allow lifecycle interface not implemented
            '@angular-eslint/use-lifecycle-interface': 'off',
            // Allow input rename
            '@angular-eslint/no-input-rename': 'off',
            // Allow output on prefix
            '@angular-eslint/no-output-on-prefix': 'off',
            // Allow no empty object type
            '@typescript-eslint/no-empty-object-type': 'off',
            'no-console': [
                'error',
                {
                    allow: [
                        'log',
                        'warn',
                        'dir',
                        'timeLog',
                        'assert',
                        'clear',
                        'count',
                        'countReset',
                        'group',
                        'groupEnd',
                        'table',
                        'dirxml',
                        'error',
                        'groupCollapsed',
                        'Console',
                        'profile',
                        'profileEnd',
                        'timeStamp',
                        'context',
                    ],
                },
            ],
            'no-debugger': 'error',
            'no-unused-labels': 'error',
            'no-var': 'error',
            quotes: ['error', 'single'],
            curly: 'error',
            eqeqeq: ['error', 'smart'],
            'guard-for-in': 'error',
            'no-bitwise': 'error',
            'no-caller': 'error',
            'no-eval': 'off',
            'no-fallthrough': 'error',
            'no-new-wrappers': 'error',
            'no-restricted-imports': 'error',
            'no-throw-literal': 'off',
            'no-trailing-spaces': 'error',
            'no-undef-init': 'error',
            'no-underscore-dangle': 'off',
            'no-unused-expressions': 'warn',
            'valid-typeof': 'error',
            'no-useless-escape': 'warn',
            'no-case-declarations': 'warn',
            'no-extra-boolean-cast': 'warn',
            '@typescript-eslint/no-wrapper-object-types': 'warn',
        },
    },
    {
        files: ['src/**/*.html'],
        languageOptions: {
            parser: angularTemplateParser,
        },
        plugins: {
            '@angular-eslint/template': angularTemplateEslint,
        },
        rules: {
            // ...angularTemplateEslint.configs.recommended.rules,
            // Accessibility rules
            '@angular-eslint/template/click-events-have-key-events': 'warn',
            '@angular-eslint/template/alt-text': 'warn',
            '@angular-eslint/template/interactive-supports-focus': 'warn',
            '@angular-eslint/template/mouse-events-have-key-events': 'warn',
        },
    },
    {
        ignores: ['src/assets/metronic/**', 'typings*.d.ts', 'app-component-base.ts'],
    },
];
