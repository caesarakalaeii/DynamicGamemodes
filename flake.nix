{
  # Keep this line accurate and one line long: `nix flake metadata` prints it,
  # and it is the first thing a cold agent learns about the repo.
  description = "DynamicGamemodes -- BattleBit Remastered API modules that rotate custom game modes. Run `nix flake show` for the command map.";

  # nixpkgs is the only input, on purpose.
  #
  # flake-utils would buy exactly one thing here -- eachDefaultSystem -- which is
  # the three-line genAttrs below. In exchange it costs a second lock node in
  # every repo (flake-utils transitively pulls `systems`, so really two), a
  # second upstream that can break one repo and not the other forty, and a
  # hardcoded system list this repo cannot edit. That list is currently broken:
  # it still contains x86_64-darwin, which now throws (see `systems` below).
  #
  # nixos-unstable is the same channel the author's own NixOS config tracks, so
  # `nix develop` here and `nixos-rebuild` there resolve the same store paths and
  # share one cache.
  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";

  outputs =
    # `...` rather than a closed { self, nixpkgs }: adding a second input later
    # would otherwise fail with "called with unexpected argument 'self'".
    { nixpkgs, ... }:
    let
      lib = nixpkgs.lib;

      # x86_64-darwin is deliberately absent. nixpkgs 26.11 replaced that whole
      # attribute set with `throw "Nixpkgs 26.11 has dropped support for
      # x86_64-darwin"`. genAttrs is lazy, so plain `nix develop` on Linux would
      # not notice -- it detonates later, on `nix flake check --all-systems`.
      # Add it back only against a separate nixpkgs-26.05-darwin input.
      systems = [
        "x86_64-linux"
        "aarch64-linux"
        "aarch64-darwin"
      ];

      # Stand-in for flake-utils.lib.eachDefaultSystem. Passes `pkgs` rather than
      # a system string, because that is what every call site below wants.
      forAllSystems = f: lib.genAttrs systems (system: f nixpkgs.legacyPackages.${system});

      # ======================================================================
      # PER-REPO BLOCK 1 -- the toolchain
      # ======================================================================
      # Everything the commands below need. `nix flake check` realises this
      # closure, so a typo'd attr name fails at the flake gate instead of
      # surfacing as "command not found" halfway through a task.
      #
      # Explicit `pkgs.foo`, never `with pkgs; [ ... ]`: when an attr disappears
      # in a nixpkgs bump, `with` reports a bare undefined identifier with no
      # hint of which set it came from, and the name is not greppable.
      #
      # Both csproj files say <TargetFramework>net6.0</TargetFramework>, and
      # net6.0 is EOL. That is NOT a reason to reach for
      # dotnetCorePackages.sdk_6_0: it is marked insecure on this pin and
      # refuses to evaluate, and unblocking it needs `permittedInsecurePackages`,
      # which forces `import nixpkgs { config = ...; }` and abandons the clean
      # single-input legacyPackages path. dotnet-sdk_9 builds net6.0 fine -- the
      # net6.0 reference assemblies come down from NuGet as
      # Microsoft.NETCore.App.Ref during restore -- and that is verified working
      # on this repo (`dotnet build DynamicGamemodes.sln` -> DynamicGamemodes.dll,
      # 11 warnings, 0 errors). The right long-term fix is retargeting the two
      # csproj files to net8.0/net9.0, which is a source change and out of scope
      # for this flake.
      #
      # Pinned by MAJOR on purpose: the bare `dotnet-sdk` alias is still 8.0.423,
      # so it is not even the version you would assume, and an alias that moves
      # under you invalidates every obj/ in the fleet on the same afternoon.
      toolchain = pkgs: [
        # ---- this repo's ecosystem ----
        # Carries `dotnet format` in-box, so lint and fmt need no extra package.
        pkgs.dotnet-sdk_9

        # ---- present in every repo in the fleet ----
        pkgs.git
        pkgs.jq
        pkgs.gnumake
      ];

      # ======================================================================
      # PER-REPO BLOCK 2 -- libraries that get dlopened, not linked
      # ======================================================================
      # Empty, and verified empty rather than assumed: nixpkgs patchelfs the .NET
      # host and runtime with an RPATH covering icu, openssl and libunwind, so
      # even invoking "$DOTNET_ROOT/dotnet --info" with LD_LIBRARY_PATH unset
      # prints its version fine. There are no pip wheels or node prebuilds in
      # this repo to need the usual stdenv.cc.cc.lib crutch. Leaving this empty
      # also means the ambient LD_LIBRARY_PATH is left completely untouched --
      # add an entry only when something here actually fails to dlopen.
      nativeLibs = pkgs: [ ];

      # ======================================================================
      # PER-REPO BLOCK 3 -- constant environment variables
      # ======================================================================
      # Only values that are constants belong here. Anything that must READ an
      # existing value (LD_LIBRARY_PATH), UNSET something (SOURCE_DATE_EPOCH) or
      # touch the work tree goes in the shellHook further down.
      #
      # This attrset is applied to BOTH surfaces -- the dev shell and every
      # `nix run` wrapper -- so a command cannot behave differently depending on
      # how it was invoked.
      envVars = pkgs: {
        # nixpkgs' `dotnet` is a wrapper that sets DOTNET_ROOT for itself only,
        # so anything invoked AROUND it -- an MSBuild task that shells out, a
        # language server, dotnet-ef -- sees nothing. Set it ambiently.
        DOTNET_ROOT = "${pkgs.dotnet-sdk_9}/share/dotnet";
        DOTNET_CLI_TELEMETRY_OPTOUT = "1";
        # `dotnet new` rejects a --nologo flag, which is why this is an env var
        # and not just an argument on each command below.
        DOTNET_NOLOGO = "1";
        # Stops persistent MSBuild worker processes from pinning a stale store
        # path across a nixpkgs bump, which shows up as a build that keeps using
        # an SDK you no longer have in your shell.
        MSBUILDDISABLENODEREUSE = "1";
      };

      # ======================================================================
      # PER-REPO BLOCK 4 -- the command map
      # ======================================================================
      # THE single source of truth. It generates `apps` (so `nix run .#build`
      # works), the `dev-*` wrappers on PATH inside the shell, and `dev-help`.
      # Nothing is written twice, so `nix flake show` can never disagree with
      # what `dev-build` actually runs.
      #
      # `test` and `run` are deliberately ABSENT, and their absence is
      # information: there is no test project anywhere in the solution, and this
      # builds a class LIBRARY (DynamicGamemodes.dll, no OutputType, no entry
      # point) that BattleBitAPIRunner loads into a BattleBit Remastered server.
      # `dotnet run` cannot start it, and a stub verb that echoed "not
      # applicable" would only turn this map into a liar. Add `test` here the day
      # a real test project lands in the solution.
      #
      # Every command names the solution explicitly, and via $REPO_ROOT so it
      # also works from a subdirectory. This is not decoration: BOTH csproj files
      # sit in the repo root next to the .sln, so an argument-less `dotnet build`
      # dies with "MSBUILD : error MSB1011: Specify which project or solution file
      # to use because this folder contains more than one project or solution
      # file." Note the solution references DynamicGamemodes.csproj only --
      # GameModeModule.csproj is orphaned, and because it shares the directory it
      # would glob the same .cs files into a second assembly through the same
      # obj/ tree. Do not "fix" that by adding it to these commands.
      commands = pkgs: {
        setup = {
          description = "(network) restore NuGet packages for the solution";
          text = ''dotnet restore "$REPO_ROOT/DynamicGamemodes.sln" "$@"'';
        };
        build = {
          # `dotnet build` restores implicitly, so on a cold NuGet cache this
          # needs the network even though `setup` exists; pass --no-restore to
          # keep it offline once `setup` has run.
          description = "build the solution (implicitly restores unless --no-restore)";
          text = ''dotnet build --nologo "$REPO_ROOT/DynamicGamemodes.sln" "$@"'';
        };
        lint = {
          # --no-restore because linting should not silently hit the network; run
          # `setup` first. This currently reports real WHITESPACE drift across the
          # tracked .cs files and exits non-zero -- that is the honest state of
          # the repo, not a broken command. `dev-fmt` is what fixes it.
          description = "dotnet format --verify-no-changes (needs `setup` first)";
          text = ''
            dotnet format "$REPO_ROOT/DynamicGamemodes.sln" --verify-no-changes --no-restore "$@"
          '';
        };
        fmt = {
          description = "dotnet format (rewrites .cs files in place; needs `setup` first)";
          text = ''dotnet format "$REPO_ROOT/DynamicGamemodes.sln" --no-restore "$@"'';
        };
      };

      # ======================================================================
      # GENERIC MACHINERY -- byte-identical in all 41 repos, do not edit
      # ======================================================================

      # Prepend, never assign: a host LD_LIBRARY_PATH may be carrying something
      # the user needs, and clobbering it breaks binaries they launch from here.
      # Linux only -- on darwin the loader variable is DYLD_*, and exporting a
      # Linux-shaped value there is at best useless.
      ldPreamble =
        pkgs:
        lib.optionalString (pkgs.stdenv.hostPlatform.isLinux && nativeLibs pkgs != [ ]) ''
          export LD_LIBRARY_PATH="${lib.makeLibraryPath (nativeLibs pkgs)}''${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"
        '';

      # Every command gets $REPO_ROOT. `nix run` and `nix develop` both start in
      # whatever directory they were invoked from, so a bare relative path
      # silently resolves against the wrong tree as soon as an agent works from a
      # subdirectory. Note we do NOT cd there: commands act on the caller's cwd
      # on purpose.
      rootPreamble = ''
        REPO_ROOT="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
        export REPO_ROOT
      '';

      # One derivation per command, reused by both `apps` and the dev shell, so
      # the two can never diverge. `dev-` prefixed because a bare `test` binary
      # earlier on PATH would shadow the POSIX shell builtin and quietly break
      # every script in the repo that uses it.
      wrappers =
        pkgs:
        lib.mapAttrs (
          name: cmd:
          pkgs.writeShellApplication {
            name = "dev-${name}";
            runtimeInputs = toolchain pkgs;
            runtimeEnv = envVars pkgs;
            meta.description = cmd.description;
            text = ''
              ${rootPreamble}
              ${ldPreamble pkgs}
              ${cmd.text}
            '';
          }
        ) (commands pkgs);

      helpFor =
        pkgs:
        let
          cmds = commands pkgs;
          names = lib.attrNames cmds;
          width = lib.foldl' (a: n: lib.max a (builtins.stringLength n)) 0 names;
          pad = n: n + lib.concatStrings (lib.genList (_: " ") (width - builtins.stringLength n));
          line = n: c: "  dev-${pad n}  ${c.description}";
        in
        pkgs.writeShellApplication {
          name = "dev-help";
          meta.description = "print this repo's command map (works offline)";
          text = ''
            cat <<'EOF'
            ${lib.concatStringsSep "\n" (lib.mapAttrsToList line cmds)}
            EOF
          '';
        };
    in
    {
      # `nix flake show` -- the discovery entrypoint, and deliberately the whole
      # machine-facing contract: every app carries a meta.description, which
      # `nix flake show` prints inline and `nix flake show --json` exposes at
      # .apps.<system>.<name>.description. Pure evaluation, so an agent gets the
      # entire command map in one cheap call without reading a README.
      #
      # Do NOT invent a top-level output for this (`agentManifest`, `probeThing`
      # ...). Nix answers with `warning: unknown flake output '<name>'` on every
      # single `nix flake check`, forever.
      apps = forAllSystems (
        pkgs:
        lib.mapAttrs (name: cmd: {
          type = "app";
          program = "${(wrappers pkgs).${name}}/bin/dev-${name}";
          meta.description = cmd.description;
        }) (commands pkgs)
      );

      # `nix develop` -- the toolchain, plus a dev-<verb> for every app.
      devShells = forAllSystems (pkgs: {
        default = pkgs.mkShell {
          packages = toolchain pkgs ++ lib.attrValues (wrappers pkgs) ++ [ (helpFor pkgs) ];

          env = envVars pkgs;

          # Some C extensions and node-gyp addons compile at -O0, where glibc's
          # _FORTIFY_SOURCE becomes a hard error instead of a warning.
          hardeningDisable = [ "fortify" ];

          shellHook = ''
            # mkShell inherits SOURCE_DATE_EPOCH=315532800 (1980-01-01) from
            # stdenv, and any wheel or zip built in here then dies with "ZIP does
            # not support timestamps before 1980".
            unset SOURCE_DATE_EPOCH

            ${rootPreamble}
            ${ldPreamble pkgs}

            # Nothing networked, nothing stateful and nothing interactive above
            # this line, and nothing below it either. No venv creation, no
            # `npm install`, no `dotnet restore`, no `read`, no `exec $SHELL`.
            # Bootstrapping in the hook makes a cold `nix develop -c dotnet build`
            # start downloading before it runs anything, on EVERY invocation --
            # the exact failure an unattended agent cannot diagnose. That is what
            # `dev-setup` is for.

            # The banner is interactive-only, and this guard is load-bearing:
            # shellHook output lands on the STDOUT of `nix develop -c <cmd>`, so
            # an unguarded echo corrupts anything parsing it
            # (`nix develop -c cat x.json | jq` fails to parse). $- is the only
            # reliable discriminator here -- it lacks `i` for `nix develop -c`
            # and has it at an interactive prompt. Do not test $PS1 (unset in
            # both) or $IN_NIX_SHELL (set in both). >&2 is the second layer, for
            # the case where a caller runs us on a pty.
            case $- in
              *i*) echo "DynamicGamemodes dev shell -- 'dev-help' for the command map" >&2 ;;
            esac
          '';
        };
      });

      # `nix flake check` -- honest by construction. It realises the toolchain
      # closure (so a typo'd or currently-broken attr fails here) and builds
      # every wrapper, which runs shellcheck over every command text. Add real
      # test derivations beside it. NEVER add a check that always passes: an
      # agent reads "all checks passed!" as a signal, and a fake check makes
      # `nix flake check` a liar.
      #
      # It deliberately does NOT try to build the solution: `dotnet restore`
      # needs the network and a writable $HOME for ~/.nuget, neither of which
      # exists in the nix build sandbox. NuGet restore is the part of this repo
      # that cannot be made hermetic, and pretending otherwise here would just
      # produce a check that fails for reasons unrelated to the code.
      checks = forAllSystems (pkgs: {
        toolchain =
          pkgs.runCommand "toolchain-check"
            {
              nativeBuildInputs = toolchain pkgs ++ lib.attrValues (wrappers pkgs);
            }
            ''
              for verb in ${lib.escapeShellArgs (lib.attrNames (commands pkgs))}; do
                command -v "dev-$verb" > /dev/null || {
                  echo "dev-$verb is not on PATH" >&2
                  exit 1
                }
              done
              touch "$out"
            '';
      });

      # `nix fmt` -- formats the *Nix* in this repo; project code is `dev-fmt`.
      # nixfmt-tree (the treefmt wrapper) rather than bare nixfmt, because bare
      # nixfmt tries to parse every path handed to it and fails on non-Nix files.
      # This file ships already formatted, so `nix fmt` is a no-op rather than a
      # diff in 41 repos.
      formatter = forAllSystems (pkgs: pkgs.nixfmt-tree);
    };
}
