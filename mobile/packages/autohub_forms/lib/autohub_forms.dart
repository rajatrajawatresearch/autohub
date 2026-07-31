/// The Form Schema Registry's Flutter renderer.
///
/// Nobody in this workspace writes a form by hand. `GET /platform/forms/{key}` returns a document
/// and `SchemaForm` renders it — the sell flow, dealer onboarding, the 200-point inspection
/// checklist and the loan application are one widget walking different JSON.
///
/// The public surface is deliberately narrow: the widget, its controller, the contract types, the
/// pure validation and visibility functions, and the two ports an app must fill in (options and
/// uploads). The controls themselves are private, because the moment an app can reach one directly
/// it can render a field that never came from a registry — and then the claim this whole workspace
/// exists to prove is no longer true.
library;

export 'src/control_registry.dart'
    show
        ControlBuilder,
        ControlEntry,
        isSupportedControl,
        overrideControl,
        resolveControl,
        supportedControls;
export 'src/controls/media_picker.dart' show FormMediaPicker, MediaSource, defaultMediaPicker;
export 'src/controls/media_upload.dart' show MediaUploadController, PickedMedia;
export 'src/controls/shared.dart'
    show asMediaItems, mediaItemsToValue, minorToRupees, rupeesToMinor;
export 'src/field_config.dart'
    show
        AddressPart,
        FormDefinitionFields,
        FormFieldConfig,
        PickerLevel,
        SliderMark,
        ValidationRuleMeta;
export 'src/form_controller.dart' show FormSubmissionRejected, SchemaFormController;
export 'src/form_scope.dart'
    show FieldOptionsBuilder, FieldOptionsState, FieldProps, SchemaFormScope;
export 'src/schema_form.dart' show SchemaForm;
export 'src/server_errors.dart'
    show faultsFromProblem, fieldErrorsFromProblem, resolveServerFieldErrors, stepIndexForFields;
export 'src/types.dart';
export 'src/validation.dart'
    show FormValidation, fallbackFaultCode, isEmptyValue, validateField, validateForm;
export 'src/visibility.dart'
    show
        isFieldVisible,
        isStepVisible,
        isVisible,
        stripHidden,
        visibleFieldKeys,
        visibleFields,
        visibleFieldsOf,
        visibleSteps;
