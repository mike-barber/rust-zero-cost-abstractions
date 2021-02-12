//! Test various zero-cost abstractions.
//! We create a TestSet from which we sample pairs of vectors.
//!
//! Then we have various functions that calculate a number based on a pair of vectors,
//! according to these rules for vectors `va` and `vb`:
//!
//! - Start with `sum = 0`
//! - for every pair `a`, `b` in aligned vectors `va`, `vb`
//!   - if `a > 2`, then `sum += a * b`
//! - return `sum`
//
//! Pretty simple stuff, but interesting enough to demonstrate performance.

use rand::{distributions::Uniform, Rng};

/// TestSet maintains a bunch of vectors, and provides a way to
/// randomly sample pairs of them.
///
/// Create a TestSet with 100 vectors, of 20k length, then
/// sample from it.
/// ```
/// // create the test set
/// use rust_zca_bench::TestSet;
/// use rand::thread_rng;
///
/// let mut rng = thread_rng();
/// let test_set = TestSet::create(20_000, 100, &mut rng);
/// assert_eq!(100, test_set.num_vecs());
/// assert_eq!(20_000, test_set.vec_length());
///
/// // sample from it
/// let (v1,v2) = test_set.sample_pair(&mut rng);
/// assert_eq!(20_000, v1.len());
/// assert_eq!(20_000, v2.len());
/// ```
pub struct TestSet {
    vectors: Vec<Vec<i32>>,
}
impl TestSet {
    pub fn create<R>(vec_length: usize, num_vecs: usize, rng: &mut R) -> TestSet
    where
        R: Rng,
    {
        let uniform = Uniform::new(0, 10);
        let mut vecs = Vec::new();
        for _ in 0..num_vecs {
            let v = rng.sample_iter(uniform).take(vec_length).collect();
            vecs.push(v);
        }
        TestSet { vectors: vecs }
    }

    pub fn sample_pair<R>(&self, rng: &mut R) -> (&[i32], &[i32])
    where
        R: Rng,
    {
        use rand::seq::SliceRandom;

        let vv1 = self.vectors.choose(rng).unwrap();
        let vv2 = self.vectors.choose(rng).unwrap();
        (vv1, vv2)
    }

    pub fn num_vecs(&self) -> usize {
        self.vectors.len()
    }
    pub fn vec_length(&self) -> usize {
        self.vectors.first().unwrap().len()
    }
}

/// really old-school directly indexed loop
pub fn calculate_direct_index(slice_a: &[i32], slice_b: &[i32]) -> i64 {
    if slice_a.len() != slice_b.len() {
        panic!("slice length mismatch")
    }

    let len = slice_a.len();
    let aa = &slice_a[0..len];
    let bb = &slice_b[0..len];
    
    let mut res = 0;
    for i in 0..len {
        if aa[i] > 2 {
            res += aa[i] as i64 * bb[i] as i64;
        }
    }
    res
}

/// imperative calculation by looping and adding, but still using iterators
pub fn calculate_direct(slice_a: &[i32], slice_b: &[i32]) -> i64 {
    let mut res = 0;
    for (a, b) in slice_a.iter().zip(slice_b.iter()) {
        if *a > 2 {
            res += *a as i64 * *b as i64;
        }
    }
    res
}

/// functional calculation using filter_map to simultaneously filter and map
/// values. Each pair returns an Option.
pub fn calculate_iter(slice_a: &[i32], slice_b: &[i32]) -> i64 {
    slice_a
        .iter()
        .zip(slice_b.iter())
        .filter_map(|(a, b)| match *a > 2 {
            true => Some(*a as i64 * *b as i64),
            false => None,
        })
        .sum()
}

/// functional calculation using a fold
pub fn calculate_fold(slice_a: &[i32], slice_b: &[i32]) -> i64 {
    slice_a
        .iter()
        .zip(slice_b.iter())
        .fold(0_i64, |acc, (a, b)| match *a > 2 {
            true => acc + (*a as i64 * *b as i64),
            false => acc,
        })
}

#[cfg(test)]
mod tests {
    use super::*;

    const EXPECTED_RESULT: i64 = 900;
    fn reference_vecs() -> (Vec<i32>, Vec<i32>) {
        (
            vec![1, 2, 3, 4, 5, 6, 7, 8, 9, 10],
            vec![11, 12, 13, 14, 15, 16, 17, 18, 19, 20],
        )
    }

    #[test]
    fn calculate_direct_correct() {
        let (a, b) = reference_vecs();
        let res = calculate_direct(&a, &b);
        assert_eq!(EXPECTED_RESULT, res, "direct was {}", res);
    }

    
    #[test]
    fn calculate_direct_index_correct() {
        let (a, b) = reference_vecs();
        let res = calculate_direct_index(&a, &b);
        assert_eq!(EXPECTED_RESULT, res, "direct was {}", res);
    }

    #[test]
    fn calculate_iter_correct() {
        let (a, b) = reference_vecs();
        let res = calculate_iter(&a, &b);
        assert_eq!(EXPECTED_RESULT, res, "iter was {}", res);
    }

    #[test]
    fn calculate_fold_correct() {
        let (a, b) = reference_vecs();
        let res = calculate_iter(&a, &b);
        assert_eq!(EXPECTED_RESULT, res, "fold was {}", res);
    }
}
