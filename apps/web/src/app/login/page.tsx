import type { Metadata } from 'next';
import { OtpLogin } from '@/components/auth/otp-login';

export const metadata: Metadata = {
  title: 'Sign in',
  description: 'Sign in with your phone number. No password.',
};

export default function LoginPage() {
  return (
    <div className="mx-auto flex max-w-[var(--container-content)] justify-center px-4 py-20 sm:px-6">
      <OtpLogin />
    </div>
  );
}
