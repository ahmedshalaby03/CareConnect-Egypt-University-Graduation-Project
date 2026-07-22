import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Group-level validator that also stamps the error on the confirmation control, so the
 * message can appear under the field the user is actually looking at.
 */
export function matchFields(sourceName: string, confirmationName: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const source = group.get(sourceName);
    const confirmation = group.get(confirmationName);

    if (!source || !confirmation || !confirmation.value) {
      return null;
    }

    if (source.value === confirmation.value) {
      // Clear only our own error; leave anything else the control has.
      if (confirmation.hasError('fieldsMismatch')) {
        const { fieldsMismatch, ...rest } = confirmation.errors ?? {};
        confirmation.setErrors(Object.keys(rest).length ? rest : null);
      }

      return null;
    }

    confirmation.setErrors({ ...(confirmation.errors ?? {}), fieldsMismatch: true });
    return { fieldsMismatch: true };
  };
}

/**
 * Mirrors the server's password policy so the user is told about a weak password before a
 * round trip. The API remains the authority.
 */
export function strongPassword(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value as string | null;

    if (!value) {
      return null;
    }

    const failures: string[] = [];

    if (value.length < 8) {
      failures.push('at least 8 characters');
    }
    if (!/[A-Z]/.test(value)) {
      failures.push('an uppercase letter');
    }
    if (!/[a-z]/.test(value)) {
      failures.push('a lowercase letter');
    }
    if (!/[0-9]/.test(value)) {
      failures.push('a digit');
    }
    if (!/[^a-zA-Z0-9]/.test(value)) {
      failures.push('a special character');
    }

    return failures.length ? { weakPassword: failures } : null;
  };
}
