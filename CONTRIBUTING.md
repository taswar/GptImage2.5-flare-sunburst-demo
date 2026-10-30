# Contributing

Thanks for helping improve this sample. It's a .NET 10 console app with five use cases for the GPT Image 2.5 Flare and Sunburst deployments on Azure OpenAI: an iterative kitchen edit, travel previews, lesson diagrams, a campaign banner with catalog shots, and a virtual try-on. Contributions that make it clearer, more correct, or more useful to other .NET developers are welcome.

## Ways to contribute

- **Report a bug.** A case fails, or the README doesn't match what the code does.
- **Fix the docs.** Typos, unclear steps, or a missing troubleshooting case (for example, an API version that returns 404 for edits).
- **Improve the sample.** For example, running one case from the command line, or Entra ID sign-in as an alternative to the API key.
- **Share results.** If you compared Flare and Sunburst on your own prompts, an issue describing the quality and speed you saw helps others.

For anything larger than a small fix, open an issue first so we can agree on the approach before you write code.

## Set up

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
2. Clone the repo and build it:

   ```bash
   git clone https://github.com/taswar/GptImage2.5-flare-sunburst-demo.git
   cd GptImage2.5-flare-sunburst-demo
   dotnet build
   ```

3. To run it, deploy `gpt-image-2.5-flare` and `gpt-image-2.5-sunburst` on an Azure OpenAI resource, then set `AZURE_AI_ENDPOINT` and `AZURE_AI_API_KEY` with user secrets, as described in the [README](README.md#azure-setup-and-credentials). A full run makes 13 image calls, so it takes a while and costs money.

## Guidelines

- **Never commit secrets.** No API keys, endpoints or tokens in code, config files, screenshots or error output. Keys belong in user secrets or a secret store.
- **Use Flare and Sunburst for their jobs.** Flare is for previews and batches, Sunburst for final assets and product-photo edits. Keep new cases consistent with that split, or explain why not.
- **Keep the edit details.** Azure OpenAI edits take the image as a form field named `image[]` and need an API version that supports edits. Keep the error output that shows the status, URL and response body, without the key.
- **Use images you may share.** Images go into a public repo. Only add images you made or have the rights to, and no photos of real customers. The try-on photo in the repo is AI-generated.
- **Keep committed images small.** Keep any image you add under 2 MB, and update the README gallery if you replace an output.
- **Keep it a sample.** The code should stay short and readable in one `Program.cs`. Prefer clarity over abstraction, and add a dependency only when it clearly helps.
- **Match the existing style.** Target `net10.0`, keep nullable reference types on, and follow the layout already in `Program.cs`.
- **Keep the README in sync.** If you add a case, change settings or change outputs, update the README in the same pull request.
- **One change per pull request.** Small, focused pull requests are easier to review and merge.

## Pull request checklist

- [ ] `dotnet build` succeeds with no new warnings.
- [ ] Any case you changed runs against your own deployments, or you explain why you couldn't run it.
- [ ] No keys, endpoints or personal data are in the diff.
- [ ] The README reflects any change in cases, settings or outputs.
- [ ] The pull request description says what changed and why.

The build workflow runs on every pull request and has to pass before a merge.

## Reporting a bug

Open an issue and include:

- Which case failed and what you expected.
- The HTTP status and response body from the error, with the endpoint and key removed.
- The API version you used for edits.
- Your .NET SDK version (`dotnet --version`) and operating system.

## Security issues

Please don't report security problems in a public issue. Use **Security → Report a vulnerability** on the repo to send a private report.

## License

By contributing, you agree that your contributions are licensed under the [MIT License](LICENSE) that covers this project.
