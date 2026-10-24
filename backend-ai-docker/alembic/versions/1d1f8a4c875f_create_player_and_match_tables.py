"""create_player_and_match_tables

Revision ID: 1d1f8a4c875f
Revises: 
Create Date: 2026-10-24 17:13:54.358975

"""
from typing import Sequence, Union

from alembic import op
import sqlalchemy as sa


# revision identifiers, used by Alembic.
revision: str = '1d1f8a4c875f'
down_revision: Union[str, Sequence[str], None] = None
branch_labels: Union[str, Sequence[str], None] = None
depends_on: Union[str, Sequence[str], None] = None


def upgrade() -> None:
    # 1. Create players table
    op.create_table(
        'players',
        sa.Column('id', sa.Uuid(), nullable=False),
        sa.Column('gmail', sa.String(length=255), nullable=False),
        sa.Column('username', sa.String(length=50), nullable=False),
        sa.Column('avatar_url', sa.String(length=255), server_default='profile_avatar_round.png', nullable=False),
        sa.Column('level', sa.Integer(), server_default='1', nullable=False),
        sa.Column('exp', sa.Integer(), server_default='0', nullable=False),
        sa.Column('gold', sa.Integer(), server_default='500', nullable=False),
        sa.Column('gems', sa.Integer(), server_default='50', nullable=False),
        sa.Column('energy', sa.Integer(), server_default='100', nullable=False),
        sa.Column('created_at', sa.DateTime(timezone=True), server_default=sa.text('CURRENT_TIMESTAMP'), nullable=False),
        sa.Column('updated_at', sa.DateTime(timezone=True), server_default=sa.text('CURRENT_TIMESTAMP'), nullable=False),
        sa.PrimaryKeyConstraint('id')
    )
    op.create_index(op.f('ix_players_gmail'), 'players', ['gmail'], unique=True)
    op.create_index(op.f('ix_players_username'), 'players', ['username'], unique=True)

    # 2. Create player_stats table
    op.create_table(
        'player_stats',
        sa.Column('id', sa.Uuid(), nullable=False),
        sa.Column('player_id', sa.Uuid(), nullable=False),
        sa.Column('elo', sa.Integer(), server_default='1200', nullable=False),
        sa.Column('matches_played', sa.Integer(), server_default='0', nullable=False),
        sa.Column('wins', sa.Integer(), server_default='0', nullable=False),
        sa.Column('losses', sa.Integer(), server_default='0', nullable=False),
        sa.Column('draws', sa.Integer(), server_default='0', nullable=False),
        sa.Column('win_streak', sa.Integer(), server_default='0', nullable=False),
        sa.Column('highest_elo', sa.Integer(), server_default='1200', nullable=False),
        sa.Column('updated_at', sa.DateTime(timezone=True), server_default=sa.text('CURRENT_TIMESTAMP'), nullable=False),
        sa.ForeignKeyConstraint(['player_id'], ['players.id'], ondelete='CASCADE'),
        sa.PrimaryKeyConstraint('id'),
        sa.UniqueConstraint('player_id')
    )
    op.create_index(op.f('ix_player_stats_elo'), 'player_stats', ['elo'], unique=False)
    op.create_index(op.f('ix_player_stats_player_id'), 'player_stats', ['player_id'], unique=True)

    # 3. Create match_history table
    op.create_table(
        'match_history',
        sa.Column('id', sa.Uuid(), nullable=False),
        sa.Column('match_code', sa.String(length=64), nullable=False),
        sa.Column('player_id', sa.Uuid(), nullable=False),
        sa.Column('opponent_name', sa.String(length=100), server_default='Cyber AI', nullable=False),
        sa.Column('is_ai', sa.Boolean(), server_default='0', nullable=False),
        sa.Column('player_role', sa.String(length=10), server_default='white', nullable=False),
        sa.Column('result', sa.String(length=10), nullable=False),
        sa.Column('score', sa.String(length=10), server_default='1 - 0', nullable=False),
        sa.Column('elo_change', sa.Integer(), server_default='0', nullable=False),
        sa.Column('moves_count', sa.Integer(), server_default='0', nullable=False),
        sa.Column('duration_sec', sa.Float(), server_default='0.0', nullable=False),
        sa.Column('ended_at', sa.DateTime(timezone=True), server_default=sa.text('CURRENT_TIMESTAMP'), nullable=False),
        sa.ForeignKeyConstraint(['player_id'], ['players.id'], ondelete='CASCADE'),
        sa.PrimaryKeyConstraint('id')
    )
    op.create_index(op.f('ix_match_history_ended_at'), 'match_history', ['ended_at'], unique=False)
    op.create_index(op.f('ix_match_history_match_code'), 'match_history', ['match_code'], unique=False)
    op.create_index(op.f('ix_match_history_player_id'), 'match_history', ['player_id'], unique=False)


def downgrade() -> None:
    op.drop_index(op.f('ix_match_history_player_id'), table_name='match_history')
    op.drop_index(op.f('ix_match_history_match_code'), table_name='match_history')
    op.drop_index(op.f('ix_match_history_ended_at'), table_name='match_history')
    op.drop_table('match_history')

    op.drop_index(op.f('ix_player_stats_player_id'), table_name='player_stats')
    op.drop_index(op.f('ix_player_stats_elo'), table_name='player_stats')
    op.drop_table('player_stats')

    op.drop_index(op.f('ix_players_username'), table_name='players')
    op.drop_index(op.f('ix_players_gmail'), table_name='players')
    op.drop_table('players')
