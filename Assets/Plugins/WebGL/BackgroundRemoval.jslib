
mergeInto(LibraryManager.library, {
  RemoveBackgroundAndUpload: function (
    imagePtr,
    imageLength,
    projectUrlPtr,
    anonKeyPtr,
    callbackObjectPtr,
    callbackMethodPtr
  ) {
    const imageBytes = HEAPU8.slice(
      imagePtr,
      imagePtr + imageLength
    );

    const projectUrl = UTF8ToString(projectUrlPtr);
    const anonKey = UTF8ToString(anonKeyPtr);
    const callbackObject = UTF8ToString(callbackObjectPtr);
    const callbackMethod = UTF8ToString(callbackMethodPtr);

    function report(result) {
      SendMessage(
        callbackObject,
        callbackMethod,
        JSON.stringify(result)
      );
    }

    async function run() {
      try {
        // Load the SDK if it hasn't been loaded yet.
        if (!window.BgRemove) {
          if (!window.bgRemoveScriptPromise) {
            window.bgRemoveScriptPromise = new Promise(
              (resolve, reject) => {
                const script = document.createElement("script");
                script.src = "https://bgremovers.org/sdk/v1.js";
                script.onload = resolve;
                script.onerror = () =>
                  reject(new Error("Could not load BgRemove SDK"));
                document.head.appendChild(script);
              }
            );
          }

          await window.bgRemoveScriptPromise;
        }

        console.log("BgRemove SDK loaded:", BgRemove.version);

        // Convert captured photo into a Blob.
        const photoBlob = new Blob(
          [imageBytes],
          { type: "image/jpeg" }
        );

        // Remove background.
        const transparentPng =
          await BgRemove.removeBackground(photoBlob, {
            maxDimension: 2048,
            onProgress: (p) => console.log("BgRemove:", p)
          });

        console.log("Background removed!");

        // Create a unique PNG filename.
        const assetPath =
          crypto.randomUUID() + ".png";

        // Upload transparent PNG to Supabase.
        const uploadUrl =
          projectUrl.replace(/\/$/, "") +
          "/storage/v1/object/quest_assets/" +
          assetPath;

        const response = await fetch(uploadUrl, {
          method: "POST",
          headers: {
            "apikey": anonKey,
            "Authorization": "Bearer " + anonKey,
            "Content-Type": "image/png"
          },
          body: transparentPng
        });

        if (!response.ok) {
          throw new Error(
            "Supabase upload failed: " +
            response.status + " " +
            await response.text()
          );
        }

        console.log("Collectible saved:", assetPath);

        report({
          success: true,
          asset_path: assetPath
        });

      } catch (error) {
        console.error("Background removal failed:", error);

        report({
          success: false,
          error: error.message || String(error)
        });
      }
    }

    run();
  }
});