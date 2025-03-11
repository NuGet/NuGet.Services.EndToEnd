# Introduction

This project contains end-to-end tests for NuGet.org and its internal services.

End-to-end tests are meant to simulate customer scenarios and verify artifacts and results that customers care about.
Testing of edge cases or implementation details should be done in component-specific functional or unit test suites.

## Local Execution

1. Create an account on the environment you wish to test.
2. Create a new API key on this account.
3. Modify `src\NuGet.Services.EndToEnd\Support\TestSettings.cs`:
    1. Change the `CurrentMode` property to the environment you wish to target.
    2. Inside of the `Create` method, find the instantiation of `TestSettings` that corresponds to the environment you wish to test.
    Replace the `TestSetting`'s `"API_KEY"` string with the API key you created in step #2.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow Microsoft’s Trademark & Brand Guidelines. Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party’s policies.

ttt