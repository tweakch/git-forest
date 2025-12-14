# Manual Tests

This directory contains manual test scripts for git-forest CLI functionality.

## Plan Installation Tests

### Test Scripts

1. **`test-plan-installation.sh`** - Automated test suite
   - Runs 14 comprehensive tests
   - Validates all aspects of plan installation CLI
   - Provides pass/fail reporting with color-coded output
   - Returns exit code 0 if all tests pass

2. **`test-plan-installation-interactive.sh`** - Interactive demonstration
   - Demonstrates the complete user experience
   - Shows both human-readable and JSON output formats
   - Tests multiple plan sources and categories
   - Provides detailed step-by-step walkthrough

3. **`PLAN_INSTALLATION_TEST_REPORT.md`** - Comprehensive test report
   - Documents all test results
   - Provides user experience assessment
   - Includes recommendations for future enhancements
   - Shows example outputs and command usage

### Running the Tests

```bash
# Run automated test suite
./tests/manual/test-plan-installation.sh

# Run interactive demonstration
./tests/manual/test-plan-installation-interactive.sh

# View test report
cat tests/manual/PLAN_INSTALLATION_TEST_REPORT.md
```

### Prerequisites

- git-forest must be built: `dotnet build`
- The test scripts use absolute paths to the CLI binary
- Tests create temporary directories in `/tmp` and clean up after themselves

### Test Coverage

The tests validate:
- ✅ Forest initialization
- ✅ Status checking (human and JSON formats)
- ✅ Plan listing
- ✅ Plan installation from local paths
- ✅ Plan installation from GitHub slugs (stubbed)
- ✅ Plan installation from URLs (stubbed)
- ✅ Help system and documentation
- ✅ JSON output for automation
- ✅ Multiple plan categories

### Test Results

**Status:** All tests passing (14/14)  
**Last Run:** 2025-12-14  
**Exit Code:** 0 (Success)

### Notes

- Tests use the pre-defined plan catalog from `config/plans/`
- Some functionality is currently stubbed (state persistence, actual file operations)
- Tests verify CLI interface and output formats, not full implementation
- Future tests should be added as implementation progresses

## Adding New Tests

When adding new manual tests:

1. Create a new `.sh` script in this directory
2. Make it executable: `chmod +x script-name.sh`
3. Follow the existing patterns for output and error handling
4. Update this README with test description and usage
5. Add test results to a markdown report if appropriate

## Automation

These manual tests can be integrated into CI/CD pipelines:

```yaml
- name: Run manual tests
  run: |
    cd tests/manual
    ./test-plan-installation.sh
```

The tests return appropriate exit codes for CI systems.
