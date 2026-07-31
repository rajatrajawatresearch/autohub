import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_forms/autohub_forms.dart';

/// The taxonomy and geography half of `SchemaForm`'s two ports.
///
/// `autohub_forms` renders a picker and refuses to know where its options came from — that single
/// function type is the no-hardcoding rule expressed in the type system, because there is nowhere
/// in the renderer for a list of fuel types to live. This is the app's answer, and it is nothing
/// more than a switchboard onto `autohub_core`'s registry cache: disk first, revalidate behind, so
/// an owner filling in a supply post beside their vehicle in a basement still gets a permit-class
/// list.
///
/// Three ways a field says where its options are, in the order they are checked:
///
/// 1. **`optionsFromAttribute`** — a taxonomy attribute code. This is the one that matters. A row
///    added to `platform.codes` becomes an option here on the next launch, which is exactly the
///    claim `docs/10-mobile-architecture.md` §1 makes and the reason none of the nine attachment
///    contract types is spelled anywhere in this binary.
/// 2. **`optionsSource`** — a named registry read, for the lists that are not attributes: makes,
///    models, variants, serviceable cities.
/// 3. **the cascading level's own key** — what `vehicle_picker` and `address` send when the
///    registry row named a level but no explicit source.
///
/// An address this build does not recognise resolves to an **empty list, not an exception**. The
/// distinction is deliberate and the control's own vocabulary makes it: "Options did not load,
/// retry" is a promise that retrying helps, and it does not — a source this build has never heard
/// of is a registry running ahead of a deployed client, which no amount of retrying fixes. The
/// server's `422` on submit is authoritative, and the partner can finish the field on the web.
FormOptionsLoader registryOptionsLoader(RegistryClient registries, {String? categoryCode}) {
  return (OptionsRequest request) async {
    final String? attribute = request.attribute;
    if (attribute != null) {
      return _attributeOptions(
        registries,
        attribute: attribute,
        categoryCode: _str(request.params['category']) ?? categoryCode,
      );
    }

    final String address = request.source ?? request.level ?? '';
    return switch (address) {
      'taxonomy.makes' || 'make' => _nodes(
          await registries.makes(categoryCode: _str(request.params['category']) ?? categoryCode),
        ),
      'taxonomy.models' || 'model' => await _dependent(
          _str(request.params['make']),
          (String make) async => _nodes(await registries.models(make)),
        ),
      'taxonomy.variants' || 'variant' => await _dependent(
          _str(request.params['model']),
          (String model) async => _nodes(
            await registries.variants(model, year: _int(request.params['year'])),
          ),
        ),
      'geo.cities' || 'city' => _cities(await registries.cities()),
      _ => const <Option>[],
    };
  };
}

/// One taxonomy attribute's option list.
///
/// Returns empty when the category is unknown rather than fetching every category's attributes:
/// `GET /platform/taxonomy/attributes` is scoped by category by design, and guessing one would
/// offer a truck's permit classes to somebody listing a sedan.
Future<List<Option>> _attributeOptions(
  RegistryClient registries, {
  required String attribute,
  String? categoryCode,
}) async {
  if (categoryCode == null || categoryCode.isEmpty) return const <Option>[];

  final AttributesResponse response = await registries.attributes(categoryCode);
  for (final TaxonomyAttribute definition in response.attributes) {
    if (definition.code == attribute) return definition.options ?? const <Option>[];
  }
  return const <Option>[];
}

/// Runs [fetch] only when the level it depends on has been chosen.
///
/// A model list with no make is a meaningless query, and issuing it anyway spends a round trip to
/// receive every model on the platform — which is both slow and, on a picker, wrong.
Future<List<Option>> _dependent(
  String? dependency,
  Future<List<Option>> Function(String value) fetch,
) async {
  if (dependency == null || dependency.isEmpty) return const <Option>[];
  return fetch(dependency);
}

List<Option> _nodes(TaxonomyNodesResponse response) => <Option>[
      for (final TaxonomyNode node in response.items) Option(value: node.code, label: node.name),
    ];

/// Only serviceable cities reach a picker.
///
/// A partner who selects a city the platform does not operate in has been sent down a flow that
/// ends in a support call, and the geography registry is the only thing that knows which is which.
List<Option> _cities(CitiesResponse response) => <Option>[
      for (final City city in response.items)
        if (city.serviceable) Option(value: city.code, label: city.name),
    ];

String? _str(Object? value) => value is String && value.isNotEmpty ? value : null;

int? _int(Object? value) {
  if (value is int) return value;
  if (value is String) return int.tryParse(value.trim());
  return null;
}
