import { Avatar, Card, LoadingState, PageHeader, useBank } from "@minibank/shared/src/index";
import "./profile.css";

/** Feature: profile — read-only personal banking information. */
export function ProfileScreen(): React.JSX.Element {
  const { profile, loading, error } = useBank();
  if (loading) return <LoadingState message="Loading profile…" />;
  if (error || !profile) {
    return (
      <div className="mobile-screen">
        <Card>
          <p role="alert">{error ?? "Could not load profile."}</p>
        </Card>
      </div>
    );
  }
  return (
    <div className="mobile-screen">
      <PageHeader title="My Profile" subtitle="Read-only banking info." />
      <Card>
        <div className="profile-hero ocean-placeholder">
          <Avatar name={profile.fullName} size={60} />
          <p className="profile-name">{profile.fullName}</p>
          <p className="profile-account">{profile.accountNumber}</p>
        </div>
        <dl className="profile-rows">
          <div>
            <dt>Full Name</dt>
            <dd>{profile.fullName}</dd>
          </div>
          <div>
            <dt>Account Number</dt>
            <dd className="mono">{profile.accountNumber}</dd>
          </div>
          <div>
            <dt>Phone Number</dt>
            <dd>{profile.phoneNumber}</dd>
          </div>
        </dl>
      </Card>
    </div>
  );
}
