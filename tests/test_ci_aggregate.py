import copy
import importlib.util
import pathlib
import unittest

SPEC = importlib.util.spec_from_file_location(
    'progo_ci_aggregate', pathlib.Path(__file__).resolve().parents[1] / 'scripts' / 'check_ci_aggregate.py')
GATE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(GATE)


PARTITIONS = ('vpn-server-test', 'windows-build', 'installer-tests')


class AggregateTests(unittest.TestCase):
    def setUp(self):
        self.sha = 'a' * 40
        self.needs = {name: {'result': 'success', 'outputs': {'tested_sha': self.sha}}
                      for name in PARTITIONS}

    def test_exact_required_partitions(self):
        self.assertEqual(GATE.REQUIRED, PARTITIONS)

    def test_all_exact_success(self):
        GATE.validate(self.needs, self.sha)

    def test_any_non_success(self):
        for name in PARTITIONS:
            for state in ('failure', 'cancelled', 'skipped', 'in_progress', None):
                with self.subTest(job=name, state=state):
                    needs = copy.deepcopy(self.needs)
                    needs[name]['result'] = state
                    with self.assertRaises(ValueError):
                        GATE.validate(needs, self.sha)

    def test_missing_partition(self):
        for name in PARTITIONS:
            needs = copy.deepcopy(self.needs)
            del needs[name]
            with self.assertRaises(ValueError):
                GATE.validate(needs, self.sha)

    def test_extra_partition(self):
        self.needs['unapproved'] = self.needs[PARTITIONS[0]]
        with self.assertRaises(ValueError):
            GATE.validate(self.needs, self.sha)

    def test_each_wrong_commit(self):
        for name in PARTITIONS:
            needs = copy.deepcopy(self.needs)
            needs[name]['outputs']['tested_sha'] = 'b' * 40
            with self.assertRaises(ValueError):
                GATE.validate(needs, self.sha)

    def test_each_missing_commit(self):
        for name in PARTITIONS:
            needs = copy.deepcopy(self.needs)
            del needs[name]['outputs']['tested_sha']
            with self.assertRaises(ValueError):
                GATE.validate(needs, self.sha)

    def test_invalid_expected_commit(self):
        for sha in (None, '', 'a' * 39, 'a' * 41, '../invalid', 'A' * 40):
            with self.assertRaises(ValueError):
                GATE.validate(self.needs, sha)

    def test_invalid_needs(self):
        for needs in (None, [], '', 0):
            with self.assertRaises(ValueError):
                GATE.validate(needs, self.sha)

    def test_invalid_job(self):
        self.needs[PARTITIONS[0]] = None
        with self.assertRaises(ValueError):
            GATE.validate(self.needs, self.sha)

    def test_missing_outputs(self):
        del self.needs[PARTITIONS[0]]['outputs']
        with self.assertRaises(ValueError):
            GATE.validate(self.needs, self.sha)

    def test_invalid_outputs(self):
        self.needs[PARTITIONS[0]]['outputs'] = []
        with self.assertRaises(ValueError):
            GATE.validate(self.needs, self.sha)


if __name__ == '__main__':
    unittest.main()
