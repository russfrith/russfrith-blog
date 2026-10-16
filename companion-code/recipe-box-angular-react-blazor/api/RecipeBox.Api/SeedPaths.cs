namespace RecipeBox.Api;

// Where seed data/images and the live image store live. Seed sources are
// baked read-only into the API image (or bind-mounted for dev); the image
// store sits on the recipebox-data named volume. See spec.md "Image rules"
// and "Reset" behavior.
public record SeedPaths(string SeedRoot, string ImageRoot);
