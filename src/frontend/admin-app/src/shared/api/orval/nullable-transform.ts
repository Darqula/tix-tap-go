import { defineTransformer } from "orval";

/**
 * Orval cannot properly handle type: ["some-type", "null"], instead it generates
 * a separate file with new type with union (some-type | null), which significantly
 * increases model file count.
 * This transformer replaces "null" within type array with the dedicated "nullable=true"
 */
export default defineTransformer((inputSchema) => {
  const resultingScema = structuredClone(inputSchema);
  for (const schema of Object.values(resultingScema.components?.schemas ?? {})) {
    for (const property of Object.values(schema.properties ?? {})) {
      if (!isMultiTypeProperty(property)) {
        continue;
      }
      if (property.type.includes("null")) {
        property.type = property.type.filter((type) => type !== "null");
        property.nullable = true;
      }
    }
  }
  return resultingScema;
});

const isMultiTypeProperty = (
  property: unknown,
): property is {
  nullable?: boolean;
  type: string[];
} => {
  return (
    !!property &&
    typeof property === "object" &&
    "type" in property &&
    Array.isArray(property.type) &&
    typeof property.type[0] === "string"
  );
};
