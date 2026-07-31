/**
 * `@autohub/forms` — the Form Schema Registry's frontend.
 *
 * The bet this package represents: nobody in AutoHub ever writes a form by hand again. The sell
 * flow, dealer onboarding, the 200-point inspection checklist and the loan application are one
 * component rendering four JSON documents, and adding a field to any of them is a database row.
 *
 * The three entry points, in the order you will need them:
 *
 *   <SchemaForm definition={…} onSubmit={…} />   the whole thing, multi-step, validated
 *   useSchemaForm({ definition, onSubmit })      the same wiring, your own chrome
 *   buildFormSchema(definition, { values })      validation with no React, for the server
 */

export { SchemaForm, type SchemaFormProps } from './schema-form';

export {
  useSchemaForm,
  createSchemaResolver,
  initialValues,
  type SchemaFormApi,
  type UseSchemaFormArgs,
} from './use-schema-form';

export {
  SchemaFormServicesProvider,
  useSchemaFormServices,
  useFormLabels,
  useUploader,
  useFieldOptions,
  type SchemaFormServices,
  type SchemaFormServicesProviderProps,
  type FieldOptionsState,
  type UseFieldOptionsArgs,
} from './form-context';

export {
  controlRegistry,
  resolveControl,
  isSupportedControl,
  registerControl,
  type ControlEntry,
} from './control-registry';

export {
  buildFieldSchema,
  buildFormSchema,
  compileRules,
  isEmptyValue,
  issueCode,
  resolveServerFieldErrors,
  validateAgainstSchema,
  FALLBACK_ISSUE_CODE,
  ISSUE_CODE_PARAM,
  type BuildFormSchemaOptions,
} from './zod-from-schema';

export {
  isFieldVisible,
  isStepVisible,
  isVisible,
  stripHidden,
  visibleFieldKeys,
  visibleFields,
  visibleFieldsOf,
  visibleSteps,
} from './visibility';

export {
  createLabelResolver,
  defaultFormLabels,
  interpolate,
  type AddressPart,
  type ControlComponent,
  type ControlName,
  type FieldConfig,
  type FieldDefinition,
  type FieldFault,
  type FieldOption,
  type FieldProps,
  type FileUploader,
  type FormDefinition,
  type FormLabelKey,
  type FormLabels,
  type FormValues,
  type LabelResolver,
  type MediaItem,
  type OptionsLoader,
  type OptionsRequest,
  type PickerLevel,
  type StepDefinition,
  type UploadRequest,
  type UploadResult,
  type UploadStatus,
  type ValidationRule,
  type ValidationRuleName,
  type VisibilityRule,
} from './types';

/* The controls themselves, for a portal that composes its own layout around one of them. */
export {
  AddressControl,
  CheckboxControl,
  DateControl,
  FileControl,
  MoneyControl,
  MultiselectControl,
  NumberControl,
  OtpControl,
  PhoneControl,
  PhotoSetControl,
  PlateControl,
  RadioControl,
  RichTextControl,
  SelectControl,
  SliderControl,
  TextControl,
  ToggleControl,
  UnsupportedControl,
  VehiclePickerControl,
  useMediaUpload,
  type MediaUploadApi,
} from './controls';
