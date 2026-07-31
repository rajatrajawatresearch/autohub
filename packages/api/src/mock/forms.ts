/**
 * Form Schema Registry fixtures.
 *
 * These are what the single form renderer consumes. `sell_vehicle` is deliberately the richest:
 * conditional EV fields, a plate control, a photo shot-list, and per-rule error codes — everything
 * `SchemaForm` has to handle, present on day one.
 */

import type { z } from 'zod';
import type { FormDefinitionSchema } from '../schemas';

type FormDefinitionInput = z.input<typeof FormDefinitionSchema>;

const SELL_VEHICLE: FormDefinitionInput = {
  key: 'sell_vehicle',
  version: 7,
  title: 'Tell us about your car',
  description: 'We pull most of this from the RTO record. Correct anything that looks wrong.',
  submitLabel: 'Get my price',
  steps: [
    {
      key: 'identity',
      title: 'Your car',
      description: 'We pull most of this from the RTO record.',
      fields: [
        {
          key: 'registration_number',
          label: 'Registration number',
          control: 'plate',
          required: true,
          placeholder: 'MH 12 AB 1234',
          helpText: 'Exactly as printed on the plate.',
          validation: [
            {
              rule: 'pattern',
              value: '^[A-Z]{2}[0-9]{1,2}[A-Z]{0,3}[0-9]{1,4}$',
              code: 'registration.malformed',
              message: 'Check the digits — example MH12AB1234.',
            },
          ],
        },
        {
          key: 'city',
          label: 'City',
          control: 'select',
          required: true,
          placeholder: 'Where is the car now?',
          optionsFromAttribute: null,
          options: [
            { value: 'pune', label: 'Pune' },
            { value: 'mumbai', label: 'Mumbai' },
            { value: 'bengaluru', label: 'Bengaluru' },
            { value: 'delhi', label: 'Delhi' },
            { value: 'hyderabad', label: 'Hyderabad' },
            { value: 'ahmedabad', label: 'Ahmedabad' },
          ],
          validation: [
            { rule: 'required', code: 'city.required', message: 'Pick the city the car is in.' },
          ],
        },
        {
          key: 'fuel_type',
          label: 'Fuel',
          control: 'radio',
          required: true,
          optionsFromAttribute: 'fuel_type',
          validation: [
            { rule: 'required', code: 'fuel_type.required', message: 'Select the fuel type.' },
          ],
        },
        {
          key: 'kms_driven',
          label: 'Kms driven',
          control: 'number',
          unit: 'km',
          required: true,
          placeholder: '38400',
          validation: [
            {
              rule: 'max',
              value: 500000,
              code: 'kms.too_high',
              message: 'Enter the odometer reading in kilometres.',
            },
            { rule: 'min', value: 0, code: 'kms.negative', message: 'Kms driven cannot be negative.' },
          ],
        },
        {
          key: 'owners',
          label: 'Owners',
          control: 'select',
          required: true,
          optionsFromAttribute: 'owners',
          helpText: 'As recorded on the RC.',
        },
        {
          key: 'battery_health_pct',
          label: 'Battery health',
          control: 'slider',
          unit: '%',
          required: true,
          helpText: 'From the vehicle display, or from the last service report.',
          config: { min: 40, max: 100, step: 1 },
          /* This is why adding EVs needed no code. */
          visibleWhen: { '==': [{ var: 'fuel_type' }, 'ev'] },
        },
      ],
    },
    {
      key: 'condition',
      title: 'Condition',
      description: 'Be straight about it. An inspection will find everything anyway.',
      fields: [
        {
          key: 'condition',
          label: 'Overall condition',
          control: 'radio',
          required: true,
          options: [
            { value: 'excellent', label: 'Excellent — nothing to mention' },
            { value: 'good', label: 'Good — normal wear' },
            { value: 'fair', label: 'Fair — needs minor work' },
            { value: 'poor', label: 'Poor — needs major work' },
          ],
        },
        {
          key: 'accident_history',
          label: 'Accident history',
          control: 'toggle',
          required: false,
          helpText: 'Any repair involving structural work.',
        },
        {
          key: 'accident_notes',
          label: 'What was repaired',
          control: 'rich_text',
          required: true,
          placeholder: 'Front-left panel replaced after a 2023 collision. Insurance claim settled.',
          visibleWhen: { '==': [{ var: 'accident_history' }, true] },
        },
        {
          key: 'photos',
          label: 'Photos',
          control: 'photo_set',
          required: true,
          helpText: 'Daylight, all four corners, and the odometer. Buyers skip listings without them.',
          config: {
            min: 6,
            max: 20,
            prompts: ['Front 3/4', 'Rear 3/4', 'Odometer', 'Interior', 'Engine bay', 'Tyres'],
          },
        },
      ],
    },
    {
      key: 'contact',
      title: 'Where to send the price',
      fields: [
        {
          key: 'phone',
          label: 'Mobile number',
          control: 'phone',
          required: true,
          placeholder: '98200 12345',
          helpText: 'Used for the offer and nothing else.',
          validation: [
            {
              rule: 'phone',
              code: 'phone.invalid',
              message: 'Enter a 10-digit Indian mobile number.',
            },
          ],
        },
        {
          key: 'expected_price_minor',
          label: 'Expected price',
          control: 'money',
          required: false,
          helpText: 'Optional. We will show you what the market says either way.',
          config: { currency: 'INR', minMinor: 1000000 },
        },
      ],
    },
  ],
};

const DEALER_ONBOARDING: FormDefinitionInput = {
  key: 'dealer_onboarding',
  version: 4,
  title: 'Register your dealership',
  submitLabel: 'Submit for verification',
  steps: [
    {
      key: 'business',
      title: 'Business',
      fields: [
        {
          key: 'legal_name',
          label: 'Registered business name',
          control: 'text',
          required: true,
          validation: [
            {
              rule: 'minLength',
              value: 3,
              code: 'dealer.legal_name.too_short',
              message: 'Enter the name exactly as on the GST certificate.',
            },
          ],
        },
        {
          key: 'gstin',
          label: 'GSTIN',
          control: 'text',
          required: true,
          placeholder: '27AABCU9603R1ZM',
          validation: [
            {
              rule: 'pattern',
              value: '^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$',
              code: 'payments.invoice.gst_number_invalid',
              message: 'GSTIN is 15 characters — example 27AABCU9603R1ZM.',
            },
          ],
        },
        {
          key: 'pan',
          label: 'PAN',
          control: 'text',
          required: true,
          placeholder: 'AABCU9603R',
          validation: [
            {
              rule: 'pattern',
              value: '^[A-Z]{5}[0-9]{4}[A-Z]$',
              code: 'identity.kyc.pan_invalid',
              message: 'PAN is 10 characters — example AABCU9603R.',
            },
          ],
        },
        {
          key: 'dealer_type',
          label: 'Dealership type',
          control: 'select',
          required: true,
          options: [
            { value: 'independent', label: 'Independent used-car dealer' },
            { value: 'franchise', label: 'OEM franchise / authorised' },
            { value: 'fleet', label: 'Fleet operator' },
            { value: 'aggregator', label: 'Taxi aggregator' },
          ],
        },
      ],
    },
    {
      key: 'premises',
      title: 'Premises',
      fields: [
        { key: 'address', label: 'Showroom address', control: 'address', required: true },
        {
          key: 'capacity',
          label: 'Cars on display',
          control: 'number',
          required: false,
          unit: 'cars',
        },
        {
          key: 'documents',
          label: 'Shop & establishment certificate',
          control: 'file',
          required: true,
          config: { accept: ['application/pdf', 'image/jpeg'], maxSizeMb: 8, multiple: false },
        },
      ],
    },
  ],
};

const INSPECTION_CHECKLIST: FormDefinitionInput = {
  key: 'inspection_checklist',
  version: 11,
  title: 'Inspection',
  submitLabel: 'Submit report',
  steps: [
    {
      key: 'exterior',
      title: 'Exterior',
      fields: [
        {
          key: 'paint_condition',
          label: 'Paint',
          control: 'radio',
          required: true,
          options: [
            { value: 'original', label: 'Original throughout' },
            { value: 'partial_repaint', label: 'Partial repaint' },
            { value: 'full_repaint', label: 'Full repaint' },
          ],
        },
        {
          key: 'panel_gaps',
          label: 'Panel gaps even',
          control: 'toggle',
          required: true,
        },
        {
          key: 'exterior_photos',
          label: 'Exterior photos',
          control: 'photo_set',
          required: true,
          config: {
            min: 8,
            max: 24,
            prompts: [
              'Front',
              'Front 3/4 left',
              'Left side',
              'Rear 3/4 left',
              'Rear',
              'Right side',
              'Roof',
              'Underbody',
            ],
          },
        },
      ],
    },
    {
      key: 'mechanical',
      title: 'Mechanical',
      fields: [
        {
          key: 'engine_noise',
          label: 'Engine noise at idle',
          control: 'radio',
          required: true,
          options: [
            { value: 'normal', label: 'Normal' },
            { value: 'tappet', label: 'Tappet noise' },
            { value: 'knock', label: 'Knocking' },
          ],
        },
        {
          key: 'oil_leak',
          label: 'Oil leak observed',
          control: 'toggle',
          required: true,
        },
        {
          key: 'leak_location',
          label: 'Leak location',
          control: 'text',
          required: true,
          visibleWhen: { '==': [{ var: 'oil_leak' }, true] },
        },
        {
          key: 'tyre_tread_mm',
          label: 'Minimum tread depth',
          control: 'number',
          unit: 'mm',
          required: true,
          validation: [
            {
              rule: 'min',
              value: 0,
              code: 'inspection.tread.invalid',
              message: 'Tread depth is measured in millimetres.',
            },
          ],
        },
      ],
    },
  ],
};

const LOAN_APPLICATION: FormDefinitionInput = {
  key: 'loan_application',
  version: 5,
  title: 'Apply for finance',
  submitLabel: 'Check offers',
  steps: [
    {
      key: 'applicant',
      title: 'About you',
      fields: [
        { key: 'full_name', label: 'Full name as on PAN', control: 'text', required: true },
        {
          key: 'employment_type',
          label: 'Employment',
          control: 'radio',
          required: true,
          options: [
            { value: 'salaried', label: 'Salaried' },
            { value: 'self_employed', label: 'Self-employed' },
            { value: 'business', label: 'Business owner' },
          ],
        },
        {
          key: 'monthly_income_minor',
          label: 'Monthly income',
          control: 'money',
          required: true,
          config: { currency: 'INR' },
        },
        {
          key: 'employer_name',
          label: 'Employer',
          control: 'text',
          required: true,
          visibleWhen: { '==': [{ var: 'employment_type' }, 'salaried'] },
        },
        {
          key: 'business_vintage_years',
          label: 'Years in business',
          control: 'number',
          unit: 'yrs',
          required: true,
          visibleWhen: {
            in: [{ var: 'employment_type' }, ['self_employed', 'business']],
          },
        },
      ],
    },
    {
      key: 'loan',
      title: 'Loan',
      fields: [
        {
          key: 'loan_amount_minor',
          label: 'Loan amount',
          control: 'money',
          required: true,
          config: { currency: 'INR', minMinor: 5000000 },
        },
        {
          key: 'tenure_months',
          label: 'Tenure',
          control: 'select',
          required: true,
          options: [
            { value: '12', label: '12 months' },
            { value: '24', label: '24 months' },
            { value: '36', label: '36 months' },
            { value: '48', label: '48 months' },
            { value: '60', label: '60 months' },
          ],
        },
        {
          key: 'bureau_consent',
          label: 'I allow AutoHub to check my credit report',
          control: 'checkbox',
          required: true,
          helpText: 'Required by lenders. A soft pull; it does not affect your score.',
          validation: [
            {
              rule: 'required',
              code: 'finance.bureau.consent_missing',
              message: 'Lenders cannot quote without a credit check.',
            },
          ],
        },
      ],
    },
  ],
};

export const MOCK_FORMS: Record<string, FormDefinitionInput> = {
  sell_vehicle: SELL_VEHICLE,
  dealer_onboarding: DEALER_ONBOARDING,
  inspection_checklist: INSPECTION_CHECKLIST,
  loan_application: LOAN_APPLICATION,
};
